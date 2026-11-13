using Dapper;
using DataViz.Analytics.Application.Dashboard;
using Npgsql;

namespace DataViz.Analytics.Infrastructure.ReadModel;

internal sealed class DashboardReadModel(NpgsqlDataSource dataSource) : IDashboardReadModel
{
    private const string PeriodFilter =
        "sale_date BETWEEN @From AND @To AND (@StoreId::uuid IS NULL OR store_id = @StoreId)";

    public async Task<PeriodTotals> GetTotals(DashboardPeriod period, CancellationToken cancellationToken)
    {
        const string sql = $"""
            SELECT COALESCE(SUM(gross_revenue), 0)    AS gross_revenue,
                   COALESCE(SUM(refunded_revenue), 0) AS refunded_revenue,
                   COALESCE(SUM(orders), 0)::int      AS orders,
                   COALESCE(SUM(refunds), 0)::int     AS refunds
            FROM daily_sales
            WHERE {PeriodFilter}
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleAsync<TotalsRow>(sql, Parameters(period));

        return new PeriodTotals(row.GrossRevenue, row.RefundedRevenue, row.Orders, row.Refunds);
    }

    public async Task<IReadOnlyList<DailyRevenue>> GetDailyRevenue(
        DashboardPeriod period,
        CancellationToken cancellationToken)
    {
        // generate_series keeps days without sales on the chart instead of skipping them.
        const string sql = """
            SELECT day::date                                              AS day,
                   COALESCE(SUM(s.gross_revenue - s.refunded_revenue), 0) AS revenue,
                   COALESCE(SUM(s.orders), 0)::int                        AS orders
            FROM generate_series(@From::date, @To::date, interval '1 day') AS day
            LEFT JOIN daily_sales s
                   ON s.sale_date = day::date AND (@StoreId::uuid IS NULL OR s.store_id = @StoreId)
            GROUP BY day
            ORDER BY day
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<DailyRow>(sql, Parameters(period));

        return rows.Select(row => new DailyRevenue(DateOnly.FromDateTime(row.Day), row.Revenue, row.Orders)).ToList();
    }

    public async Task<IReadOnlyList<BreakdownSlice>> GetBreakdown(
        DashboardPeriod period,
        BreakdownDimension dimension,
        CancellationToken cancellationToken)
    {
        // The column comes from a closed enum, never from user input.
        var column = dimension switch
        {
            BreakdownDimension.Category => "category",
            BreakdownDimension.Channel => "channel",
            BreakdownDimension.Store => "store_name",
            _ => throw new ArgumentOutOfRangeException(nameof(dimension), dimension, null),
        };
        var sql = $"""
            SELECT {column}                               AS label,
                   SUM(gross_revenue - refunded_revenue) AS revenue,
                   SUM(orders)::int                      AS orders
            FROM daily_sales
            WHERE {PeriodFilter}
            GROUP BY {column}
            ORDER BY revenue DESC
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<SliceRow>(sql, Parameters(period));

        return rows.Select(row => new BreakdownSlice(row.Label, row.Revenue, row.Orders)).ToList();
    }

    public async Task<IReadOnlyList<RecentSale>> GetRecentSales(int limit, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT sale_id, store_name, category, channel, amount, occurred_at, refunded_at IS NOT NULL AS refunded
            FROM sales
            ORDER BY occurred_at DESC
            LIMIT @Limit
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<RecentRow>(sql, new { Limit = limit });

        return rows
            .Select(row => new RecentSale(
                row.SaleId,
                row.StoreName,
                row.Category,
                row.Channel,
                row.Amount,
                DateTime.SpecifyKind(row.OccurredAt, DateTimeKind.Utc),
                row.Refunded))
            .ToList();
    }

    public async Task<IReadOnlyList<StoreOption>> GetStores(CancellationToken cancellationToken)
    {
        const string sql = "SELECT store_id AS id, MAX(store_name) AS name FROM daily_sales GROUP BY store_id ORDER BY name";

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<StoreRow>(sql);

        return rows.Select(row => new StoreOption(row.Id, row.Name)).ToList();
    }

    private static object Parameters(DashboardPeriod period) => new
    {
        From = period.From.ToDateTime(TimeOnly.MinValue),
        To = period.To.ToDateTime(TimeOnly.MinValue),
        period.StoreId,
    };

    private sealed class TotalsRow
    {
        public decimal GrossRevenue { get; init; }

        public decimal RefundedRevenue { get; init; }

        public int Orders { get; init; }

        public int Refunds { get; init; }
    }

    private sealed class DailyRow
    {
        public DateTime Day { get; init; }

        public decimal Revenue { get; init; }

        public int Orders { get; init; }
    }

    private sealed class SliceRow
    {
        public string Label { get; init; } = string.Empty;

        public decimal Revenue { get; init; }

        public int Orders { get; init; }
    }

    private sealed class RecentRow
    {
        public Guid SaleId { get; init; }

        public string StoreName { get; init; } = string.Empty;

        public string Category { get; init; } = string.Empty;

        public string Channel { get; init; } = string.Empty;

        public decimal Amount { get; init; }

        public DateTime OccurredAt { get; init; }

        public bool Refunded { get; init; }
    }

    private sealed class StoreRow
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}
