using CommerceHub.ProductCatalog.Domain.Products;

namespace CommerceHub.ProductCatalog.Application.Variants;

/// <summary>
/// Raw variant payload shared by create and update; validated by <see cref="VariantValidator"/>.
/// </summary>
public sealed record VariantFields(
    string? Sku,
    string? Name,
    decimal? Price,
    string? Currency,
    ProductVariantStatus? Status,
    IReadOnlyList<VariantAttributeField>? Attributes,
    IReadOnlyList<string>? ImageUrls = null);

public sealed record VariantAttributeField(long? AttributeId, string? Value);
