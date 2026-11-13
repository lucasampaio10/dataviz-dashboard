using DataViz.BuildingBlocks.Domain;
using DataViz.Sales.Domain.Stores;

namespace DataViz.Sales.Domain.Sales;

public enum ProductCategory
{
    Electronics,
    Fashion,
    Home,
    Beauty,
    Sports,
    Grocery,
}

public enum SalesChannel
{
    InStore,
    Online,
    Marketplace,
}

public enum SaleStatus
{
    Completed,
    Refunded,
}

/// <summary>
/// A completed sale. The write model only protects the rules of recording and refunding;
/// everything the dashboard shows is derived from the events it raises.
/// </summary>
public sealed class Sale : AggregateRoot<Guid>
{
    public static readonly TimeSpan RefundWindow = TimeSpan.FromDays(30);

    private Sale()
    {
    }

    public Guid StoreId { get; private set; }

    public ProductCategory Category { get; private set; }

    public SalesChannel Channel { get; private set; }

    public Money Amount { get; private set; } = Money.Zero();

    public DateTime OccurredAtUtc { get; private set; }

    public SaleStatus Status { get; private set; }

    public DateTime? RefundedAtUtc { get; private set; }

    public static Sale Record(
        Store store,
        ProductCategory category,
        SalesChannel channel,
        Money amount,
        DateTime occurredAtUtc,
        DateTime utcNow)
    {
        if (amount.Amount <= 0)
        {
            throw new DomainException("sale.invalid_amount", "A sale must be greater than zero.");
        }

        if (occurredAtUtc > utcNow)
        {
            throw new DomainException("sale.in_the_future", "A sale cannot be recorded before it happens.");
        }

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            Category = category,
            Channel = channel,
            Amount = amount,
            OccurredAtUtc = occurredAtUtc,
            Status = SaleStatus.Completed,
        };

        sale.Raise(new SaleRecordedDomainEvent(
            sale.Id,
            store.Id,
            store.Name,
            category,
            channel,
            amount,
            occurredAtUtc));

        return sale;
    }

    public void Refund(DateTime refundedAtUtc, DateTime utcNow)
    {
        if (Status == SaleStatus.Refunded)
        {
            throw new DomainException("sale.already_refunded", "This sale was already refunded.");
        }

        if (refundedAtUtc > utcNow || refundedAtUtc < OccurredAtUtc)
        {
            throw new DomainException(
                "sale.invalid_refund_date",
                "A refund happens after the sale and cannot be dated in the future.");
        }

        if (refundedAtUtc - OccurredAtUtc > RefundWindow)
        {
            throw new DomainException(
                "sale.refund_window_expired",
                $"Sales can only be refunded within {RefundWindow.Days} days.");
        }

        Status = SaleStatus.Refunded;
        RefundedAtUtc = refundedAtUtc;

        Raise(new SaleRefundedDomainEvent(Id, refundedAtUtc));
    }
}

public sealed record SaleRecordedDomainEvent(
    Guid SaleId,
    Guid StoreId,
    string StoreName,
    ProductCategory Category,
    SalesChannel Channel,
    Money Amount,
    DateTime OccurredAtUtc) : DomainEvent;

public sealed record SaleRefundedDomainEvent(Guid SaleId, DateTime RefundedAtUtc) : DomainEvent;

public interface ISaleRepository
{
    Task<Sale?> GetById(Guid id, CancellationToken cancellationToken);

    void Add(Sale sale);
}
