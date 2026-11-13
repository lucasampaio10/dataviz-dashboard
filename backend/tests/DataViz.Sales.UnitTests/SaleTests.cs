using DataViz.BuildingBlocks.Domain;
using DataViz.Sales.Domain.Sales;
using DataViz.Sales.Domain.Stores;

namespace DataViz.Sales.UnitTests;

public class SaleTests
{
    private static readonly DateTime Now = new(2026, 11, 13, 15, 0, 0, DateTimeKind.Utc);
    private static readonly Store Aldeota = Store.Open("Loja Aldeota", "Fortaleza");

    [Fact]
    public void A_recorded_sale_is_completed_and_announces_everything_a_read_model_needs()
    {
        var sale = RecordSale(occurredAtUtc: Now.AddMinutes(-5));

        Assert.Equal(SaleStatus.Completed, sale.Status);
        var recorded = Assert.IsType<SaleRecordedDomainEvent>(Assert.Single(sale.DomainEvents));
        Assert.Equal(sale.Id, recorded.SaleId);
        Assert.Equal("Loja Aldeota", recorded.StoreName);
        Assert.Equal(new Money(250m), recorded.Amount);
    }

    [Fact]
    public void A_sale_cannot_be_worth_nothing()
    {
        var exception = Assert.Throws<DomainException>(() => RecordSale(amount: 0m));

        Assert.Equal("sale.invalid_amount", exception.Code);
    }

    [Fact]
    public void A_sale_cannot_happen_in_the_future()
    {
        var exception = Assert.Throws<DomainException>(() => RecordSale(occurredAtUtc: Now.AddSeconds(1)));

        Assert.Equal("sale.in_the_future", exception.Code);
    }

    [Fact]
    public void A_recent_sale_can_be_refunded()
    {
        var sale = RecordSale(occurredAtUtc: Now.AddDays(-30));
        sale.ClearDomainEvents();

        sale.Refund(Now, Now);

        Assert.Equal(SaleStatus.Refunded, sale.Status);
        Assert.Equal(Now, sale.RefundedAtUtc);
        Assert.IsType<SaleRefundedDomainEvent>(Assert.Single(sale.DomainEvents));
    }

    [Fact]
    public void A_sale_is_refunded_only_once()
    {
        var sale = RecordSale();
        sale.Refund(Now, Now);

        var exception = Assert.Throws<DomainException>(() => sale.Refund(Now, Now));

        Assert.Equal("sale.already_refunded", exception.Code);
    }

    [Fact]
    public void A_sale_outside_the_refund_window_stays_completed()
    {
        var sale = RecordSale(occurredAtUtc: Now.AddDays(-31));

        var exception = Assert.Throws<DomainException>(() => sale.Refund(Now, Now));

        Assert.Equal("sale.refund_window_expired", exception.Code);
        Assert.Equal(SaleStatus.Completed, sale.Status);
    }

    [Fact]
    public void A_refund_reported_late_keeps_the_date_it_actually_happened()
    {
        var sale = RecordSale(occurredAtUtc: Now.AddDays(-45));
        var refundedAt = Now.AddDays(-40);

        sale.Refund(refundedAt, Now);

        Assert.Equal(refundedAt, sale.RefundedAtUtc);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-3)]
    public void A_refund_cannot_be_dated_in_the_future_or_before_the_sale(int daysFromNow)
    {
        var sale = RecordSale(occurredAtUtc: Now.AddDays(-2));

        var exception = Assert.Throws<DomainException>(() => sale.Refund(Now.AddDays(daysFromNow), Now));

        Assert.Equal("sale.invalid_refund_date", exception.Code);
    }

    private static Sale RecordSale(decimal amount = 250m, DateTime? occurredAtUtc = null) => Sale.Record(
        Aldeota,
        ProductCategory.Fashion,
        SalesChannel.Online,
        new Money(amount),
        occurredAtUtc ?? Now,
        Now);
}
