using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Products.Commands;

/// <summary>
/// Removes a product-category mapping only. Removing a mapping that does not exist succeeds.
/// </summary>
public sealed record RemoveProductCategoryCommand(Guid ProductId, long CategoryId) : ICommand<Result>;

internal sealed class RemoveProductCategoryCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<RemoveProductCategoryCommand, Result>
{
    public async Task<Result> HandleAsync(RemoveProductCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        product.RemoveCategory(command.CategoryId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
