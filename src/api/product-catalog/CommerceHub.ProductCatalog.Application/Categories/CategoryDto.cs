using CommerceHub.ProductCatalog.Domain.Categories;

namespace CommerceHub.ProductCatalog.Application.Categories;

public sealed record CategoryDto(long Id, long? ParentId, string Name, string Slug, CategoryStatus Status);

public sealed record CategoryTreeNodeDto(
    long Id,
    long? ParentId,
    string Name,
    string Slug,
    CategoryStatus Status,
    List<CategoryTreeNodeDto> Children);
