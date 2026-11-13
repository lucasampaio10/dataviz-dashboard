using DataViz.BuildingBlocks.Cqrs;
using DataViz.BuildingBlocks.Results;

namespace DataViz.Analytics.Application.Dashboard;

/// <param name="Value">Indicator in the requested period.</param>
/// <param name="Previous">Same indicator in the period of equal length right before.</param>
/// <param name="ChangePercent">Relative change; <c>null</c> when there is no previous value to compare with.</param>
public sealed record Indicator(decimal Value, decimal Previous, decimal? ChangePercent)
{
    public static Indicator Compare(decimal current, decimal previous) => new(
        current,
        previous,
        previous == 0 ? null : Math.Round((current - previous) * 100m / previous, 1));
}

public sealed record SummaryResponse(
    DateOnly From,
    DateOnly To,
    DateOnly PreviousFrom,
    DateOnly PreviousTo,
    Indicator NetRevenue,
    Indicator Orders,
    Indicator AverageTicket,
    Indicator RefundRate);

public sealed record GetSummaryQuery(DashboardPeriod Period) : IQuery<SummaryResponse>, IPeriodQuery;

internal sealed class GetSummaryValidator : PeriodQueryValidator<GetSummaryQuery>;

internal sealed class GetSummaryHandler(IDashboardReadModel readModel) : IQueryHandler<GetSummaryQuery, SummaryResponse>
{
    public async Task<Result<SummaryResponse>> Handle(GetSummaryQuery query, CancellationToken cancellationToken)
    {
        var previousPeriod = query.Period.Previous();
        var current = await readModel.GetTotals(query.Period, cancellationToken);
        var previous = await readModel.GetTotals(previousPeriod, cancellationToken);

        return new SummaryResponse(
            query.Period.From,
            query.Period.To,
            previousPeriod.From,
            previousPeriod.To,
            Indicator.Compare(current.NetRevenue, previous.NetRevenue),
            Indicator.Compare(current.Orders, previous.Orders),
            Indicator.Compare(current.AverageTicket, previous.AverageTicket),
            Indicator.Compare(current.RefundRate, previous.RefundRate));
    }
}
