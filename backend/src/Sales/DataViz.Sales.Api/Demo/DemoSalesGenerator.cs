using DataViz.BuildingBlocks.Cqrs;
using DataViz.Sales.Application.Sales;
using DataViz.Sales.Application.Stores;
using Microsoft.Extensions.Options;

namespace DataViz.Sales.Api.Demo;

internal sealed class DemoOptions
{
    public const string SectionName = "Demo";

    public bool Enabled { get; set; }

    public int HistoryDays { get; set; } = 60;

    public double RefundRate { get; set; } = 0.04;
}

/// <summary>
/// Keeps a demo environment alive: fills an empty database with a sales history and then keeps
/// selling. Every sale goes through the same command a real client would send, so the whole
/// pipeline (domain, outbox, broker, projections) is exercised.
/// </summary>
internal sealed class DemoSalesGenerator(
    IServiceScopeFactory scopeFactory,
    IOptions<DemoOptions> options,
    TimeProvider clock,
    ILogger<DemoSalesGenerator> logger) : BackgroundService
{
    private const int BackfillSeed = 20240917;
    private readonly DemoOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var stores = await Send(new ListStoresQuery(), stoppingToken);
        if (stores.Value.Count == 0)
        {
            return;
        }

        if (!await HasAnySale(stoppingToken))
        {
            await Backfill(new DemoSaleFactory(stores.Value, new Random(BackfillSeed)), stoppingToken);
        }

        await KeepSelling(new DemoSaleFactory(stores.Value, Random.Shared), stoppingToken);
    }

    private async Task Backfill(DemoSaleFactory factory, CancellationToken stoppingToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        var recorded = 0;

        for (var dayIndex = 0; dayIndex <= _options.HistoryDays; dayIndex++)
        {
            var day = today.AddDays(dayIndex - _options.HistoryDays);
            var moments = Enumerable.Range(0, factory.SalesOn(day, dayIndex))
                .Select(_ => factory.MomentDuringBusinessHours(day))
                .Where(moment => moment <= now)
                .Order();

            foreach (var moment in moments)
            {
                var sale = await Send(factory.SaleAt(moment), stoppingToken);
                if (factory.Chance(_options.RefundRate))
                {
                    var refundedAt = Earliest(factory.RefundMomentFor(moment), now);
                    await Send(new RefundSaleCommand(sale.Value, refundedAt), stoppingToken);
                }

                recorded++;
            }
        }

        logger.LogInformation("Demo history created: {Sales} sales over {Days} days.", recorded, _options.HistoryDays);
    }

    private async Task KeepSelling(DemoSaleFactory factory, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            await Task.Delay(factory.TypicalGapOn(today, _options.HistoryDays), stoppingToken);

            var now = clock.GetUtcNow().UtcDateTime;
            if (!DemoSaleFactory.IsOpenAt(now))
            {
                continue;
            }

            var sale = await Send(factory.SaleAt(now), stoppingToken);
            if (factory.Chance(_options.RefundRate))
            {
                await Send(new RefundSaleCommand(sale.Value), stoppingToken);
            }
        }
    }

    private static DateTime Earliest(DateTime first, DateTime second) => first < second ? first : second;

    private async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request, cancellationToken);
    }

    private async Task<bool> HasAnySale(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISalesQueries>().HasAnySale(cancellationToken);
    }
}

internal static class DemoExtensions
{
    public static IServiceCollection AddDemoSales(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DemoOptions>(configuration.GetSection(DemoOptions.SectionName));
        services.AddHostedService<DemoSalesGenerator>();

        return services;
    }
}
