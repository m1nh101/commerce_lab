using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Categories;
using CommerceHub.ProductCatalog.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Products.Commands;

/// <summary>
/// Assigns an existing category to a product. Assigning an already-assigned category is a no-op.
/// </summary>
public sealed record AssignProductCategoryCommand(Guid ProductId, long CategoryId) : ICommand<Result>;

internal sealed class AssignProductCategoryCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<AssignProductCategoryCommand, Result>
{
    public async Task<Result> HandleAsync(AssignProductCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        if (!await dbContext.Categories.AnyAsync(c => c.Id == command.CategoryId, cancellationToken))
        {
            return CategoryErrors.NotFound;
        }

        product.AssignCategory(command.CategoryId);

        try
        {
            return await dbContext.SaveProductChangesAsync(cancellationToken) is { } saveError ? saveError : Result.Success();
        }
        catch (ConstraintViolationException ex) when (ex.Kind == ConstraintViolationKind.Unique)
        {
            // A concurrent request inserted the same mapping; the composite key keeps the assignment idempotent.
            return Result.Success();
        }
    }
}
