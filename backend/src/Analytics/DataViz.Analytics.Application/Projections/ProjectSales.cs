using DataViz.BuildingBlocks.Messaging;
using DataViz.Contracts;

namespace DataViz.Analytics.Application.Projections;

/// <summary>
/// Write port of the read model. Each method must be idempotent per event id, because the broker
/// may deliver the same event more than once.
/// </summary>
public interface ISalesProjection
{
    Task Apply(SaleRecordedIntegrationEvent saleRecorded, CancellationToken cancellationToken);

    Task Apply(SaleRefundedIntegrationEvent saleRefunded, CancellationToken cancellationToken);
}

/// <summary>
/// Keeps the dashboard tables in step with what happened in the Sales service. This is the
/// "eventual" in eventual consistency: the read side lags the write side by one message.
/// </summary>
internal sealed class ProjectSales(ISalesProjection projection) :
    IIntegrationEventHandler<SaleRecordedIntegrationEvent>,
    IIntegrationEventHandler<SaleRefundedIntegrationEvent>
{
    public Task Handle(SaleRecordedIntegrationEvent saleRecorded, CancellationToken cancellationToken) =>
        projection.Apply(saleRecorded, cancellationToken);

    public Task Handle(SaleRefundedIntegrationEvent saleRefunded, CancellationToken cancellationToken) =>
        projection.Apply(saleRefunded, cancellationToken);
}
