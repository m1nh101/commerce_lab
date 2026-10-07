using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Attributes;
using CommerceHub.ProductCatalog.Application.Common;
using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using ProductAttribute = CommerceHub.ProductCatalog.Domain.Attributes.Attribute;

namespace CommerceHub.ProductCatalog.Application.Variants;

internal static class VariantQueryExtensions
{
    public static Task<bool> SkuExistsAsync(
        this IQueryable<ProductVariant> query,
        string sku,
        Guid? excludeId,
        CancellationToken cancellationToken) =>
        query.AnyAsync(v => v.Sku == sku && (excludeId == null || v.Id != excludeId), cancellationToken);

    public static Task<bool> AnySkuExistsAsync(
        this IQueryable<ProductVariant> query,
        IReadOnlyCollection<string> skus,
        CancellationToken cancellationToken) =>
        skus.Count == 0 ? Task.FromResult(false) : query.AnyAsync(v => skus.Contains(v.Sku), cancellationToken);

    public static async Task<bool> AllExistAsync(
        this IQueryable<ProductAttribute> query,
        IReadOnlyCollection<long> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return true;
        }

        var existing = await query.CountAsync(a => ids.Contains(a.Id), cancellationToken);
        return existing == ids.Count;
    }

    /// <summary>
    /// Checks the stored data a variant depends on: the SKU must be free and every referenced attribute must exist.
    /// </summary>
    public static async Task<Error?> CheckVariantReferencesAsync(
        this IProductCatalogDbContext dbContext,
        ValidVariant variant,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (await dbContext.ProductVariants.AsNoTracking().SkuExistsAsync(variant.Sku, excludeId, cancellationToken))
        {
            return VariantErrors.SkuConflict;
        }

        var attributeIds = variant.Attributes.Select(a => a.AttributeId).ToList();
        if (!await dbContext.Attributes.AsNoTracking().AllExistAsync(attributeIds, cancellationToken))
        {
            return AttributeErrors.NotFound;
        }

        return null;
    }

    /// <summary>
    /// Saves pending variant changes. The unique SKU index and attribute FK stay authoritative for writers that race past
    /// <see cref="CheckVariantReferencesAsync"/>; those violations are mapped by constraint name, without re-querying.
    /// </summary>
    public static async Task<Error?> SaveVariantChangesAsync(
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
        VariantConstraints.SkuUniqueIndex => VariantErrors.SkuConflict,
        VariantConstraints.AttributeForeignKey => AttributeErrors.NotFound,
        _ => null,
    };

    public static IQueryable<VariantDto> ToDto(
        this IQueryable<ProductVariant> query,
        IQueryable<ProductAttribute> attributes,
        IQueryable<ProductImage> images) =>
        query.Select(v => new VariantDto(
            v.Id,
            v.ProductId,
            v.Sku,
            v.Name,
            v.Price,
            v.Currency,
            v.Status,
            v.Attributes
                .Join(attributes, va => va.AttributeId, a => a.Id, (va, a) => new { a.Id, a.Name, a.Code, va.Value })
                .OrderBy(a => a.Name)
                .Select(a => new VariantAttributeDto(a.Id, a.Name, a.Code, a.Value))
                .ToList(),
            images
                .Where(i => i.VariantId == v.Id)
                .OrderBy(i => i.SortOrder)
                .ThenBy(i => i.Id)
                .Select(i => new VariantImageDto(i.Id, i.Url, i.SortOrder))
                .ToList(),
            v.CreatedAt,
            v.UpdatedAt));

    public static async Task<Result<VariantDto>> LoadVariantDtoAsync(
        this IProductCatalogDbContext dbContext,
        Guid id,
        CancellationToken cancellationToken)
    {
        var variant = await dbContext.ProductVariants
            .AsNoTracking()
            .Where(v => v.Id == id)
            .ToDto(dbContext.Attributes.AsNoTracking(), dbContext.ProductImages.AsNoTracking())
            .FirstOrDefaultAsync(cancellationToken);

        return variant is null ? VariantErrors.NotFound : variant;
    }
}
