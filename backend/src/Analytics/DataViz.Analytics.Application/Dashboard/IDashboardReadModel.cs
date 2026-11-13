namespace DataViz.Analytics.Application.Dashboard;

public enum BreakdownDimension
{
    Category,
    Channel,
    Store,
}

/// <summary>Raw totals of a period, as stored. The derived indicators live here, next to their definition.</summary>
public sealed record PeriodTotals(decimal GrossRevenue, decimal RefundedRevenue, int Orders, int Refunds)
{
    public decimal NetRevenue => GrossRevenue - RefundedRevenue;

    /// <summary>Average value of a sale at checkout, before any refund.</summary>
    public decimal AverageTicket => Orders == 0 ? 0m : Math.Round(GrossRevenue / Orders, 2);

    /// <summary>Share of the period's sales that were refunded, in percent.</summary>
    public decimal RefundRate => Orders == 0 ? 0m : Math.Round(Refunds * 100m / Orders, 2);
}

public sealed record DailyRevenue(DateOnly Day, decimal Revenue, int Orders);

public sealed record BreakdownSlice(string Label, decimal Revenue, int Orders);

public sealed record RecentSale(
    Guid SaleId,
    string StoreName,
    string Category,
    string Channel,
    decimal Amount,
    DateTime OccurredAtUtc,
    bool Refunded);

public sealed record StoreOption(Guid Id, string Name);

/// <summary>Query port of the read model: tables already shaped for the dashboard.</summary>
public interface IDashboardReadModel
{
    Task<PeriodTotals> GetTotals(DashboardPeriod period, CancellationToken cancellationToken);

    Task<IReadOnlyList<DailyRevenue>> GetDailyRevenue(DashboardPeriod period, CancellationToken cancellationToken);

    Task<IReadOnlyList<BreakdownSlice>> GetBreakdown(
        DashboardPeriod period,
        BreakdownDimension dimension,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RecentSale>> GetRecentSales(int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<StoreOption>> GetStores(CancellationToken cancellationToken);
}
