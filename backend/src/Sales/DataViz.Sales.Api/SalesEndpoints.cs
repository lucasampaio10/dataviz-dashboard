using DataViz.BuildingBlocks.Cqrs;
using DataViz.BuildingBlocks.Infrastructure.Web;
using DataViz.Sales.Application.Sales;
using DataViz.Sales.Application.Stores;

namespace DataViz.Sales.Api;

internal static class SalesEndpoints
{
    public static void MapSalesEndpoints(this IEndpointRouteBuilder routes)
    {
        var sales = routes.MapGroup("/api/sales").WithTags("Sales");

        sales.MapPost("/", async (RecordSaleCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(id => Results.Created($"/api/sales/{id}", new { id }));
        });

        sales.MapPost("/{id:guid}/refund", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RefundSaleCommand(id), ct);
            return result.ToHttpResult(Results.NoContent);
        });

        routes.MapGet("/api/stores", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListStoresQuery(), ct);
            return result.ToHttpResult(Results.Ok);
        }).WithTags("Stores");
    }
}
