using Dapper;
using DataViz.Analytics.Application.Projections;
using DataViz.Contracts;
using Microsoft.Extensions.Options;
using Npgsql;

namespace DataViz.Analytics.Infrastructure.ReadModel;

/// <summary>
/// Applies sales events to the read model. Each event runs in one transaction together with the
/// record that it was processed, so a redelivered event changes nothing.
/// </summary>
internal sealed class SalesProjection(NpgsqlDataSource dataSource, IOptions<AnalyticsOptions> options)
    : ISalesProjection
{
    private const string MarkAsProcessed =
        "INSERT INTO processed_events (event_id) VALUES (@EventId) ON CONFLICT DO NOTHING";

    private const string InsertSale = """
        INSERT INTO sales (sale_id, sale_date, store_id, store_name, category, channel, amount, occurred_at)
        VALUES (@SaleId, (@OccurredAtUtc AT TIME ZONE @TimeZone)::date, @StoreId, @StoreName, @Category, @Channel, @Amount, @OccurredAtUtc)
        """;

    private const string AddToDailyTotals = """
        INSERT INTO daily_sales (sale_date, store_id, store_name, category, channel, gross_revenue, orders)
        VALUES ((@OccurredAtUtc AT TIME ZONE @TimeZone)::date, @StoreId, @StoreName, @Category, @Channel, @Amount, 1)
        ON CONFLICT (sale_date, store_id, category, channel) DO UPDATE
        SET gross_revenue = daily_sales.gross_revenue + EXCLUDED.gross_revenue,
            orders = daily_sales.orders + 1,
            store_name = EXCLUDED.store_name
        """;

    private const string MarkSaleAsRefunded = """
        UPDATE sales SET refunded_at = @RefundedAtUtc
        WHERE sale_id = @SaleId AND refunded_at IS NULL
        RETURNING sale_date, store_id, category, channel, amount
        """;

    private const string AddRefundToDailyTotals = """
        UPDATE daily_sales
        SET refunded_revenue = refunded_revenue + @Amount, refunds = refunds + 1
        WHERE sale_date = @SaleDate AND store_id = @StoreId AND category = @Category AND channel = @Channel
        """;

    private readonly string _timeZone = options.Value.TimeZone;

    public async Task Apply(SaleRecordedIntegrationEvent saleRecorded, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        if (!await IsFirstDelivery(connection, transaction, saleRecorded.EventId))
        {
            return;
        }

        var parameters = new
        {
            saleRecorded.SaleId,
            saleRecorded.StoreId,
            saleRecorded.StoreName,
            saleRecorded.Category,
            saleRecorded.Channel,
            saleRecorded.Amount,
            saleRecorded.OccurredAtUtc,
            TimeZone = _timeZone,
        };
        await connection.ExecuteAsync(InsertSale, parameters, transaction);
        await connection.ExecuteAsync(AddToDailyTotals, parameters, transaction);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task Apply(SaleRefundedIntegrationEvent saleRefunded, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        if (!await IsFirstDelivery(connection, transaction, saleRefunded.EventId))
        {
            return;
        }

        var sale = await connection.QuerySingleOrDefaultAsync<RefundedSale>(
            MarkSaleAsRefunded,
            new { saleRefunded.SaleId, saleRefunded.RefundedAtUtc },
            transaction);

        if (sale is null)
        {
            // The sale has not been projected yet. Failing rolls back and lets the consumer retry.
            throw new InvalidOperationException($"Sale {saleRefunded.SaleId} is unknown to the read model.");
        }

        await connection.ExecuteAsync(AddRefundToDailyTotals, sale, transaction);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<bool> IsFirstDelivery(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid eventId) =>
        await connection.ExecuteAsync(MarkAsProcessed, new { EventId = eventId }, transaction) == 1;

    private sealed class RefundedSale
    {
        public DateTime SaleDate { get; init; }

        public Guid StoreId { get; init; }

        public string Category { get; init; } = string.Empty;

        public string Channel { get; init; } = string.Empty;

        public decimal Amount { get; init; }
    }
}
