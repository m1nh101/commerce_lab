using CommerceHub.ProductCatalog.Application.Variants;
using CommerceHub.ProductCatalog.Domain.Products;

namespace CommerceHub.ProductCatalog.Application.Products;

/// <summary>
/// Catalog view of a product. Stock quantities are owned by the stock service and are never included.
/// </summary>
public sealed record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    ProductStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Product with optionally included children; sections that were not requested are <c>null</c>.
/// </summary>
public sealed record ProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    ProductStatus Status,
    IReadOnlyList<VariantDto>? Variants,
    IReadOnlyList<ProductCategoryDto>? Categories,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ProductCategoryDto(long Id, string Name, string Slug);

[Flags]
public enum ProductIncludes
{
    None = 0,
    Variants = 1,
    Categories = 2
}

public enum ProductSort
{
    CreatedAtDesc,
    CreatedAtAsc,
    NameAsc,
    NameDesc
}
