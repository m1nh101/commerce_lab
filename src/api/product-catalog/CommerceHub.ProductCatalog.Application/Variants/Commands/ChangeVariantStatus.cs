using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Variants.Commands;

public sealed record ChangeVariantStatusCommand(Guid Id, ProductVariantStatus? Status) : ICommand<Result<VariantDto>>;

internal sealed class ChangeVariantStatusCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<ChangeVariantStatusCommand, Result<VariantDto>>
{
    public async Task<Result<VariantDto>> HandleAsync(ChangeVariantStatusCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Status is not { } status)
        {
            return Error.Validation("'status' is required.");
        }

        var variant = await dbContext.ProductVariants.FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);
        if (variant is null)
        {
            return VariantErrors.NotFound;
        }

        // Catalog lifecycle only: Active does not mean in stock.
        variant.ChangeStatus(status);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.LoadVariantDtoAsync(command.Id, cancellationToken);
    }
}
