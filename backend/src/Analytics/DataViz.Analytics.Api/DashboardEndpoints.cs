using DataViz.Analytics.Application.Dashboard;
using DataViz.BuildingBlocks.Cqrs;
using DataViz.BuildingBlocks.Infrastructure.Web;

namespace DataViz.Analytics.Api;

internal static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder routes)
    {
        var dashboard = routes.MapGroup("/api/dashboard").WithTags("Dashboard");

        dashboard.MapGet("/summary", async ([AsParameters] PeriodRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSummaryQuery(request.ToPeriod()), ct);
            return result.ToHttpResult(Results.Ok);
        });

        dashboard.MapGet("/revenue", async ([AsParameters] PeriodRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDailyRevenueQuery(request.ToPeriod()), ct);
            return result.ToHttpResult(Results.Ok);
        });

        dashboard.MapGet("/breakdown", async (
            string dimension,
            [AsParameters] PeriodRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetBreakdownQuery(request.ToPeriod(), dimension), ct);
            return result.ToHttpResult(Results.Ok);
        });

        dashboard.MapGet("/recent-sales", async (int? limit, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetRecentSalesQuery(limit ?? 10), ct);
            return result.ToHttpResult(Results.Ok);
        });

        dashboard.MapGet("/stores", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetStoresQuery(), ct);
            return result.ToHttpResult(Results.Ok);
        });
    }

    /// <summary>Query string shared by the period endpoints: <c>?from=2026-01-01&amp;to=2026-01-31&amp;storeId=</c>.</summary>
    private readonly record struct PeriodRequest(DateOnly From, DateOnly To, Guid? StoreId)
    {
        public DashboardPeriod ToPeriod() => new(From, To, StoreId);
    }
}
