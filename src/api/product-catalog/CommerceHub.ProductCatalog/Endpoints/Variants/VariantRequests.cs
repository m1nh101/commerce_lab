using CommerceHub.ProductCatalog.Domain.Products;

namespace CommerceHub.ProductCatalog.Endpoints.Variants;

public sealed record VariantAttributeRequest(long? AttributeId, string? Value);

public sealed record CreateVariantRequest(
    string? Sku,
    string? Name,
    decimal? Price,
    string? Currency,
    ProductVariantStatus? Status,
    IReadOnlyList<VariantAttributeRequest>? Attributes);

public sealed record UpdateVariantRequest(
    string? Sku,
    string? Name,
    decimal? Price,
    string? Currency,
    ProductVariantStatus? Status,
    IReadOnlyList<VariantAttributeRequest>? Attributes);

public sealed record ChangeVariantStatusRequest(ProductVariantStatus? Status);

public sealed record SetVariantAttributeRequest(string? Value);
