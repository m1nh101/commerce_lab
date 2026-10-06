using CommerceHub.ProductCatalog.Domain.Categories;

namespace CommerceHub.ProductCatalog.Endpoints.Categories;

public sealed record CreateCategoryRequest(long? ParentId, string? Name, string? Slug, CategoryStatus? Status);

/// <summary>
/// Body for PUT (full update) and PATCH (partial update).
/// </summary>
public sealed record UpdateCategoryRequest(long? ParentId, string? Name, string? Slug, CategoryStatus? Status);
