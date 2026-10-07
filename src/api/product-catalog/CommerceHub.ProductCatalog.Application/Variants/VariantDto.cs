using CommerceHub.ProductCatalog.Domain.Products;

namespace CommerceHub.ProductCatalog.Application.Variants;

/// <summary>
/// Catalog view of a variant. Stock quantities are owned by the stock service and are never included.
/// </summary>
public sealed record VariantDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string Name,
    decimal Price,
    string Currency,
    ProductVariantStatus Status,
    IReadOnlyList<VariantAttributeDto> Attributes,
    IReadOnlyList<VariantImageDto> Images,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record VariantAttributeDto(long AttributeId, string Name, string Code, string Value);

public sealed record VariantImageDto(long Id, string Url, int SortOrder);
