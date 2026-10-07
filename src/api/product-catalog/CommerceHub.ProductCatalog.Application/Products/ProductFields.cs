using CommerceHub.ProductCatalog.Domain.Products;

namespace CommerceHub.ProductCatalog.Application.Products;

/// <summary>
/// Raw product fields as received from the client, before validation.
/// </summary>
public sealed record ProductFields(string? Name, string? Slug, string? Description, ProductStatus? Status);

/// <summary>
/// Partial product update. Fields left <c>null</c> are not changed; <see cref="DescriptionSpecified"/> distinguishes
/// "clear the description" from "keep the current description".
/// </summary>
public sealed record ProductPatch(
    string? Name,
    string? Slug,
    ProductStatus? Status,
    bool DescriptionSpecified,
    string? Description);
