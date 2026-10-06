using CommerceHub.ProductCatalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Categories;

internal static class CategoryQueryExtensions
{
    public static IQueryable<Category> WhereIdOrSlug(this IQueryable<Category> query, string idOrSlug) =>
        long.TryParse(idOrSlug, out var id)
            ? query.Where(c => c.Id == id)
            : query.Where(c => c.Slug == idOrSlug);

    public static IQueryable<Category> WhereSearch(this IQueryable<Category> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var term = search.Trim().ToLower();
        return query.Where(c => c.Name.ToLower().Contains(term) || c.Slug.Contains(term));
    }

    public static IQueryable<Category> WhereStatus(this IQueryable<Category> query, CategoryStatus? status) =>
        status is null ? query : query.Where(c => c.Status == status);

    public static IQueryable<Category> WhereParent(this IQueryable<Category> query, bool parentIdSpecified, long? parentId) =>
        parentIdSpecified ? query.Where(c => c.ParentId == parentId) : query;

    public static Task<bool> SlugExistsAsync(
        this IQueryable<Category> query,
        string slug,
        long? excludeId,
        CancellationToken cancellationToken) =>
        query.AnyAsync(c => c.Slug == slug && (excludeId == null || c.Id != excludeId), cancellationToken);

    /// <summary>
    /// Loads the whole hierarchy as an <c>Id → ParentId</c> map, used to walk ancestors when checking for cycles.
    /// </summary>
    public static Task<Dictionary<long, long?>> GetParentMapAsync(
        this IQueryable<Category> query,
        CancellationToken cancellationToken) =>
        query.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.ParentId, cancellationToken);

    public static IQueryable<CategoryDto> ToDto(this IQueryable<Category> query) =>
        query.Select(c => new CategoryDto(c.Id, c.ParentId, c.Name, c.Slug, c.Status));

    public static CategoryDto ToDto(this Category category) =>
        new(category.Id, category.ParentId, category.Name, category.Slug, category.Status);
}
