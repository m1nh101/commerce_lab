using CommerceHub.ProductCatalog.Domain.Products;
using CommerceHub.ProductCatalog.Endpoints.Variants;

namespace CommerceHub.ProductCatalog.Endpoints.Products;

public sealed record CreateProductRequest(
    string? Name,
    string? Slug,
    string? Description,
    ProductStatus? Status,
    IReadOnlyList<long>? CategoryIds,
    IReadOnlyList<CreateProductVariantRequest>? Variants);

public sealed record CreateProductVariantRequest(
    string? Sku,
    string? Name,
    decimal? Price,
    string? Currency,
    ProductVariantStatus? Status,
    IReadOnlyList<VariantAttributeRequest>? Attributes);

/// <summary>
/// Body for PUT (full replace) and PATCH (partial update).
/// </summary>
public sealed record UpdateProductRequest(string? Name, string? Slug, string? Description, ProductStatus? Status);

public sealed record ChangeProductStatusRequest(ProductStatus? Status);

public sealed record ReplaceProductCategoriesRequest(IReadOnlyList<long>? CategoryIds);
