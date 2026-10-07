using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Attributes;
using CommerceHub.ProductCatalog.Application.Common;
using CommerceHub.ProductCatalog.Application.Variants;
using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Products;

internal static class ProductQueryExtensions
{
    public static Task<bool> SlugExistsAsync(
        this IQueryable<Product> query,
        string slug,
        Guid? excludeId,
        CancellationToken cancellationToken) =>
        query.AnyAsync(p => p.Slug == slug && (excludeId == null || p.Id != excludeId), cancellationToken);

    public static IQueryable<Product> WhereStatus(this IQueryable<Product> query, ProductStatus? status) =>
        status is { } s ? query.Where(p => p.Status == s) : query;

    public static IQueryable<Product> WhereCategory(this IQueryable<Product> query, long? categoryId) =>
        categoryId is { } id ? query.Where(p => p.Categories.Any(c => c.CategoryId == id)) : query;

    public static IQueryable<Product> WhereSlug(this IQueryable<Product> query, string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return query;
        }

        var value = slug.Trim();
        return query.Where(p => p.Slug == value);
    }

    public static IQueryable<Product> WhereSearch(this IQueryable<Product> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var term = search.Trim().ToLower();
        return query.Where(p => p.Name.ToLower().Contains(term) || p.Slug.Contains(term));
    }

    public static IQueryable<Product> ApplySort(this IQueryable<Product> query, ProductSort sort) => sort switch
    {
        ProductSort.NameAsc => query.OrderBy(p => p.Name).ThenBy(p => p.Id),
        ProductSort.NameDesc => query.OrderByDescending(p => p.Name).ThenByDescending(p => p.Id),
        ProductSort.CreatedAtAsc => query.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),
        _ => query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id),
    };

    public static IQueryable<ProductDto> ToDto(this IQueryable<Product> query) =>
        query.Select(p => new ProductDto(p.Id, p.Name, p.Slug, p.Description, p.Status, p.CreatedAt, p.UpdatedAt));

    public static async Task<Result<ProductDto>> LoadProductDtoAsync(
        this IProductCatalogDbContext dbContext,
        Guid id,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .ToDto()
            .FirstOrDefaultAsync(cancellationToken);

        return product is null ? ProductErrors.NotFound : product;
    }

    /// <summary>
    /// Saves pending product changes. The unique slug index and category FK stay authoritative for writers that race
    /// past the handlers' pre-checks; those violations are mapped by constraint name, without re-querying.
    /// </summary>
    public static async Task<Error?> SaveProductChangesAsync(
        this IProductCatalogDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (ConstraintViolationException ex) when (ToError(ex) is not null)
        {
            return ToError(ex);
        }
    }

    private static Error? ToError(ConstraintViolationException ex) => ex.ConstraintName switch
    {
        ProductConstraints.SlugUniqueIndex => ProductErrors.SlugConflict,
        ProductConstraints.CategoryForeignKey => ProductErrors.CategoryNotFound,
        VariantConstraints.SkuUniqueIndex => VariantErrors.SkuConflict,
        VariantConstraints.AttributeForeignKey => AttributeErrors.NotFound,
        _ => null,
    };

    /// <summary>
    /// Loads a product with the requested child sections; sections that were not requested stay <c>null</c>.
    /// </summary>
    public static async Task<Result<ProductDetailDto>> LoadProductDetailAsync(
        this IProductCatalogDbContext dbContext,
        Guid id,
        ProductIncludes includes,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .ToDto()
            .FirstOrDefaultAsync(cancellationToken);
        if (product is null)
        {
            return ProductErrors.NotFound;
        }

        IReadOnlyList<VariantDto>? variants = includes.HasFlag(ProductIncludes.Variants)
            ? await dbContext.ProductVariants
                .AsNoTracking()
                .Where(v => v.ProductId == id)
                .OrderBy(v => v.Sku)
                .ToDto(dbContext.Attributes.AsNoTracking(), dbContext.ProductImages.AsNoTracking())
                .ToListAsync(cancellationToken)
            : null;

        IReadOnlyList<ProductCategoryDto>? categories = includes.HasFlag(ProductIncludes.Categories)
            ? await dbContext.Categories
                .AsNoTracking()
                .Where(c => dbContext.Products.Any(p => p.Id == id && p.Categories.Any(pc => pc.CategoryId == c.Id)))
                .OrderBy(c => c.Name)
                .Select(c => new ProductCategoryDto(c.Id, c.Name, c.Slug))
                .ToListAsync(cancellationToken)
            : null;

        return new ProductDetailDto(
            product.Id,
            product.Name,
            product.Slug,
            product.Description,
            product.Status,
            variants,
            categories,
            product.CreatedAt,
            product.UpdatedAt);
    }
}
