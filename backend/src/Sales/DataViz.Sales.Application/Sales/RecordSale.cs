using DataViz.BuildingBlocks.Cqrs;
using DataViz.BuildingBlocks.Domain;
using DataViz.BuildingBlocks.Persistence;
using DataViz.BuildingBlocks.Results;
using DataViz.Sales.Domain.Sales;
using DataViz.Sales.Domain.Stores;
using FluentValidation;

namespace DataViz.Sales.Application.Sales;

/// <param name="OccurredAtUtc">When the sale happened; defaults to now. Lets a store upload sales late.</param>
public sealed record RecordSaleCommand(
    Guid StoreId,
    string Category,
    string Channel,
    decimal Amount,
    DateTime? OccurredAtUtc = null) : ICommand<Guid>;

internal sealed class RecordSaleValidator : AbstractValidator<RecordSaleCommand>
{
    private const decimal MaxAmount = 1_000_000m;

    public RecordSaleValidator()
    {
        RuleFor(command => command.StoreId).NotEmpty();
        RuleFor(command => command.Amount).GreaterThan(0).LessThanOrEqualTo(MaxAmount);
        RuleFor(command => command.Category).IsEnumName(typeof(ProductCategory), caseSensitive: false);
        RuleFor(command => command.Channel).IsEnumName(typeof(SalesChannel), caseSensitive: false);
    }
}

internal sealed class RecordSaleHandler(
    IStoreRepository stores,
    ISaleRepository sales,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<RecordSaleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RecordSaleCommand command, CancellationToken cancellationToken)
    {
        var store = await stores.GetById(command.StoreId, cancellationToken);
        if (store is null)
        {
            return Error.NotFound("store.not_found", $"Store {command.StoreId} was not found.");
        }

        var utcNow = clock.GetUtcNow().UtcDateTime;
        var sale = Sale.Record(
            store,
            Enum.Parse<ProductCategory>(command.Category, ignoreCase: true),
            Enum.Parse<SalesChannel>(command.Channel, ignoreCase: true),
            new Money(command.Amount),
            command.OccurredAtUtc?.ToUniversalTime() ?? utcNow,
            utcNow);

        sales.Add(sale);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return sale.Id;
    }
}
