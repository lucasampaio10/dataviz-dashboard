using DataViz.BuildingBlocks.Cqrs;
using DataViz.BuildingBlocks.Results;
using FluentValidation;

namespace DataViz.Analytics.Application.Dashboard;

public sealed record GetDailyRevenueQuery(DashboardPeriod Period) : IQuery<IReadOnlyList<DailyRevenue>>, IPeriodQuery;

internal sealed class GetDailyRevenueValidator : PeriodQueryValidator<GetDailyRevenueQuery>;

internal sealed class GetDailyRevenueHandler(IDashboardReadModel readModel)
    : IQueryHandler<GetDailyRevenueQuery, IReadOnlyList<DailyRevenue>>
{
    public async Task<Result<IReadOnlyList<DailyRevenue>>> Handle(
        GetDailyRevenueQuery query,
        CancellationToken cancellationToken) =>
        Result<IReadOnlyList<DailyRevenue>>.From(await readModel.GetDailyRevenue(query.Period, cancellationToken));
}

public sealed record BreakdownResponse(string Label, decimal Revenue, int Orders, decimal SharePercent);

public sealed record GetBreakdownQuery(DashboardPeriod Period, string Dimension)
    : IQuery<IReadOnlyList<BreakdownResponse>>, IPeriodQuery;

internal sealed class GetBreakdownValidator : PeriodQueryValidator<GetBreakdownQuery>
{
    public GetBreakdownValidator() =>
        RuleFor(query => query.Dimension)
            .IsEnumName(typeof(BreakdownDimension), caseSensitive: false)
            .WithName("dimension");
}

internal sealed class GetBreakdownHandler(IDashboardReadModel readModel)
    : IQueryHandler<GetBreakdownQuery, IReadOnlyList<BreakdownResponse>>
{
    public async Task<Result<IReadOnlyList<BreakdownResponse>>> Handle(
        GetBreakdownQuery query,
        CancellationToken cancellationToken)
    {
        var dimension = Enum.Parse<BreakdownDimension>(query.Dimension, ignoreCase: true);
        var slices = await readModel.GetBreakdown(query.Period, dimension, cancellationToken);
        var total = slices.Sum(slice => slice.Revenue);

        return slices
            .Select(slice => new BreakdownResponse(
                slice.Label,
                slice.Revenue,
                slice.Orders,
                total == 0 ? 0m : Math.Round(slice.Revenue * 100m / total, 1)))
            .ToList();
    }
}

public sealed record GetRecentSalesQuery(int Limit = 10) : IQuery<IReadOnlyList<RecentSale>>;

internal sealed class GetRecentSalesValidator : AbstractValidator<GetRecentSalesQuery>
{
    private const int MaxLimit = 50;

    public GetRecentSalesValidator() => RuleFor(query => query.Limit).InclusiveBetween(1, MaxLimit);
}

internal sealed class GetRecentSalesHandler(IDashboardReadModel readModel)
    : IQueryHandler<GetRecentSalesQuery, IReadOnlyList<RecentSale>>
{
    public async Task<Result<IReadOnlyList<RecentSale>>> Handle(
        GetRecentSalesQuery query,
        CancellationToken cancellationToken) =>
        Result<IReadOnlyList<RecentSale>>.From(await readModel.GetRecentSales(query.Limit, cancellationToken));
}

public sealed record GetStoresQuery : IQuery<IReadOnlyList<StoreOption>>;

internal sealed class GetStoresHandler(IDashboardReadModel readModel)
    : IQueryHandler<GetStoresQuery, IReadOnlyList<StoreOption>>
{
    public async Task<Result<IReadOnlyList<StoreOption>>> Handle(
        GetStoresQuery query,
        CancellationToken cancellationToken) =>
        Result<IReadOnlyList<StoreOption>>.From(await readModel.GetStores(cancellationToken));
}
