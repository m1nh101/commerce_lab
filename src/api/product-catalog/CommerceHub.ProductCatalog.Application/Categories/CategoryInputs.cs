using CommerceHub.ProductCatalog.Domain.Categories;

namespace CommerceHub.ProductCatalog.Application.Categories;

public sealed record CreateCategoryInput(string? Name, string? Slug, long? ParentId, CategoryStatus? Status);

/// <summary>
/// Fields left <c>null</c> are not changed. <see cref="ParentIdSpecified"/> distinguishes "move to root" from "keep the current parent".
/// </summary>
public sealed record UpdateCategoryInput(
    string? Name,
    string? Slug,
    CategoryStatus? Status,
    bool ParentIdSpecified,
    long? ParentId);

/// <summary>
/// When <see cref="ParentIdSpecified"/> is <c>true</c> and <see cref="ParentId"/> is <c>null</c>, only top-level categories are returned.
/// <see cref="Page"/> and <see cref="Limit"/> are ignored when building a tree.
/// </summary>
public sealed record CategoryListQuery(
    bool ParentIdSpecified,
    long? ParentId,
    CategoryStatus? Status,
    string? Search,
    int Page = 1,
    int Limit = 20);
