using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Products.Commands;

/// <summary>
/// Replaces all category mappings of a product. Every category is validated first; if any is missing nothing changes.
/// Removals and additions are written in a single <c>SaveChanges</c>, i.e. one transaction.
/// </summary>
public sealed record ReplaceProductCategoriesCommand(Guid ProductId, IReadOnlyList<long>? CategoryIds)
    : ICommand<Result<IReadOnlyList<ProductCategoryDto>>>;

internal sealed class ReplaceProductCategoriesCommandHandler(IProductCatalogDbContext dbContext)
    : ICommandHandler<ReplaceProductCategoriesCommand, Result<IReadOnlyList<ProductCategoryDto>>>
{
    public async Task<Result<IReadOnlyList<ProductCategoryDto>>> HandleAsync(
        ReplaceProductCategoriesCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.CategoryIds is null)
        {
            return Error.Validation("'category_ids' is required.");
        }

        var requested = command.CategoryIds.Distinct().ToList();

        var product = await dbContext.Products
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        var existing = await dbContext.Categories.CountAsync(c => requested.Contains(c.Id), cancellationToken);
        if (existing != requested.Count)
        {
            return ProductErrors.CategoryNotFound;
        }

        foreach (var obsolete in product.Categories.Select(c => c.CategoryId).Except(requested).ToList())
        {
            product.RemoveCategory(obsolete);
        }

        foreach (var categoryId in requested)
        {
            product.AssignCategory(categoryId);
        }

        if (await dbContext.SaveProductChangesAsync(cancellationToken) is { } saveError)
        {
            return saveError;
        }

        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(c => requested.Contains(c.Id))
            .OrderBy(c => c.Name)
            .Select(c => new ProductCategoryDto(c.Id, c.Name, c.Slug))
            .ToListAsync(cancellationToken);

        return categories;
    }
}
