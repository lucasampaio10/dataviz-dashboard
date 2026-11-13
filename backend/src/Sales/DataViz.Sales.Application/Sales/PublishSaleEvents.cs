using DataViz.BuildingBlocks.Domain;
using DataViz.BuildingBlocks.Messaging;
using DataViz.Contracts;
using DataViz.Sales.Domain.Sales;

namespace DataViz.Sales.Application.Sales;

/// <summary>
/// Translates domain events into the public contract. This is the only link between the write
/// model and the read models built from it.
/// </summary>
internal sealed class PublishSaleEvents(IIntegrationEventOutbox outbox) :
    IDomainEventHandler<SaleRecordedDomainEvent>,
    IDomainEventHandler<SaleRefundedDomainEvent>
{
    public Task Handle(SaleRecordedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Add(new SaleRecordedIntegrationEvent(
            domainEvent.SaleId,
            domainEvent.StoreId,
            domainEvent.StoreName,
            domainEvent.Category.ToString(),
            domainEvent.Channel.ToString(),
            domainEvent.Amount.Amount,
            domainEvent.Amount.Currency,
            domainEvent.OccurredAtUtc));

        return Task.CompletedTask;
    }

    public Task Handle(SaleRefundedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Add(new SaleRefundedIntegrationEvent(domainEvent.SaleId, domainEvent.RefundedAtUtc));

        return Task.CompletedTask;
    }
}
