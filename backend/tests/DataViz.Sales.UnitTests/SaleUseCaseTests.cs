using DataViz.BuildingBlocks.Messaging;
using DataViz.BuildingBlocks.Persistence;
using DataViz.BuildingBlocks.Results;
using DataViz.Contracts;
using DataViz.Sales.Application.Sales;
using DataViz.Sales.Domain.Sales;
using DataViz.Sales.Domain.Stores;
using NSubstitute;

namespace DataViz.Sales.UnitTests;

public class SaleUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 15, 0, 0, TimeSpan.Zero);

    private readonly IStoreRepository _stores = Substitute.For<IStoreRepository>();
    private readonly ISaleRepository _sales = Substitute.For<ISaleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FixedClock _clock = new(Now);

    [Fact]
    public async Task Recording_a_sale_without_a_date_uses_the_current_time()
    {
        var store = Stored(Store.Open("Loja Aldeota", "Fortaleza"));
        Sale? recorded = null;
        _sales.Add(Arg.Do<Sale>(sale => recorded = sale));

        var result = await RecordHandler().Handle(
            new RecordSaleCommand(store.Id, "fashion", "ONLINE", 199.90m),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(recorded);
        Assert.Equal(Now.UtcDateTime, recorded.OccurredAtUtc);
        Assert.Equal(ProductCategory.Fashion, recorded.Category);
        Assert.Equal(SalesChannel.Online, recorded.Channel);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Recording_a_sale_for_an_unknown_store_reports_not_found()
    {
        var result = await RecordHandler().Handle(
            new RecordSaleCommand(Guid.NewGuid(), "Fashion", "Online", 199.90m),
            CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
        _sales.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public async Task Refunding_an_unknown_sale_reports_not_found()
    {
        var result = await new RefundSaleHandler(_sales, _unitOfWork, _clock)
            .Handle(new RefundSaleCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task A_recorded_sale_is_published_with_readable_category_and_channel()
    {
        var outbox = Substitute.For<IIntegrationEventOutbox>();
        var sale = Sale.Record(
            Store.Open("Loja Aldeota", "Fortaleza"),
            ProductCategory.Electronics,
            SalesChannel.Marketplace,
            new BuildingBlocks.Domain.Money(1200m),
            Now.UtcDateTime,
            Now.UtcDateTime);
        var recorded = (SaleRecordedDomainEvent)sale.DomainEvents.Single();

        await new PublishSaleEvents(outbox).Handle(recorded, CancellationToken.None);

        outbox.Received(1).Add(Arg.Is<SaleRecordedIntegrationEvent>(integrationEvent =>
            integrationEvent.SaleId == sale.Id
            && integrationEvent.Category == "Electronics"
            && integrationEvent.Channel == "Marketplace"
            && integrationEvent.Amount == 1200m));
    }

    private RecordSaleHandler RecordHandler() => new(_stores, _sales, _unitOfWork, _clock);

    private Store Stored(Store store)
    {
        _stores.GetById(store.Id, Arg.Any<CancellationToken>()).Returns(store);
        return store;
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
