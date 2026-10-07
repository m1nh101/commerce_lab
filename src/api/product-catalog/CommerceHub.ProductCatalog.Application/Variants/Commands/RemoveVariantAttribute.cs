using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Variants.Commands;

public sealed record RemoveVariantAttributeCommand(Guid Id, long AttributeId) : ICommand<Result>;

internal sealed class RemoveVariantAttributeCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<RemoveVariantAttributeCommand, Result>
{
    public async Task<Result> HandleAsync(RemoveVariantAttributeCommand command, CancellationToken cancellationToken = default)
    {
        var variant = await dbContext.ProductVariants
            .Include(v => v.Attributes)
            .FirstOrDefaultAsync(v => v.Id == command.Id, cancellationToken);
        if (variant is null)
        {
            return VariantErrors.NotFound;
        }

        if (variant.Attributes.All(a => a.AttributeId != command.AttributeId))
        {
            return VariantErrors.AttributeNotAssigned;
        }

        variant.RemoveAttribute(command.AttributeId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
