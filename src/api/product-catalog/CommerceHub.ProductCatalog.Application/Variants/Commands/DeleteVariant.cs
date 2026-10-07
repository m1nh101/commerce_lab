using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Variants.Commands;

public sealed record DeleteVariantCommand(Guid Id) : ICommand<Result>;

internal sealed class DeleteVariantCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<DeleteVariantCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteVariantCommand command, CancellationToken cancellationToken = default)
    {
        var variant = await dbContext.ProductVariants.FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);
        if (variant is null)
        {
            return VariantErrors.NotFound;
        }

        // Attribute values and variant images are removed by cascading FKs in the same statement; the product remains.
        // External inventory records are never touched.
        dbContext.ProductVariants.Remove(variant);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
