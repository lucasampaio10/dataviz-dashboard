using DataViz.BuildingBlocks.Cqrs;
using DataViz.BuildingBlocks.Persistence;
using DataViz.BuildingBlocks.Results;
using DataViz.Sales.Domain.Sales;

namespace DataViz.Sales.Application.Sales;

/// <param name="RefundedAtUtc">When the refund happened; defaults to now. Lets a store report refunds late.</param>
public sealed record RefundSaleCommand(Guid SaleId, DateTime? RefundedAtUtc = null) : ICommand;

internal sealed class RefundSaleHandler(ISaleRepository sales, IUnitOfWork unitOfWork, TimeProvider clock)
    : ICommandHandler<RefundSaleCommand>
{
    public async Task<Result> Handle(RefundSaleCommand command, CancellationToken cancellationToken)
    {
        var sale = await sales.GetById(command.SaleId, cancellationToken);
        if (sale is null)
        {
            return Error.NotFound("sale.not_found", $"Sale {command.SaleId} was not found.");
        }

        var utcNow = clock.GetUtcNow().UtcDateTime;
        sale.Refund(command.RefundedAtUtc?.ToUniversalTime() ?? utcNow, utcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
