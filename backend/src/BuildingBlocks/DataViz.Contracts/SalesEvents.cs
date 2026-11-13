using DataViz.BuildingBlocks.Messaging;

namespace DataViz.Contracts;

/// <summary>
/// A sale was completed. Carries everything a read model needs, so consumers never have to call
/// the Sales service back to understand the event.
/// </summary>
public sealed record SaleRecordedIntegrationEvent(
    Guid SaleId,
    Guid StoreId,
    string StoreName,
    string Category,
    string Channel,
    decimal Amount,
    string Currency,
    DateTime OccurredAtUtc) : IntegrationEvent;

public sealed record SaleRefundedIntegrationEvent(Guid SaleId, DateTime RefundedAtUtc) : IntegrationEvent;
