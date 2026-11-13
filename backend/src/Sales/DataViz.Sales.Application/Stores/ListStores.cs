using DataViz.BuildingBlocks.Cqrs;
using DataViz.BuildingBlocks.Results;

namespace DataViz.Sales.Application.Stores;

public sealed record StoreResponse(Guid Id, string Name, string City);

/// <summary>Read side of the Sales service: plain projections, no aggregates involved.</summary>
public interface ISalesQueries
{
    Task<IReadOnlyList<StoreResponse>> ListStores(CancellationToken cancellationToken);

    Task<bool> HasAnySale(CancellationToken cancellationToken);
}

public sealed record ListStoresQuery : IQuery<IReadOnlyList<StoreResponse>>;

internal sealed class ListStoresHandler(ISalesQueries queries)
    : IQueryHandler<ListStoresQuery, IReadOnlyList<StoreResponse>>
{
    public async Task<Result<IReadOnlyList<StoreResponse>>> Handle(
        ListStoresQuery query,
        CancellationToken cancellationToken) =>
        Result<IReadOnlyList<StoreResponse>>.From(await queries.ListStores(cancellationToken));
}
