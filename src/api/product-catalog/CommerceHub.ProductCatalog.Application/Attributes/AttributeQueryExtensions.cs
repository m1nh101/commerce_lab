using Microsoft.EntityFrameworkCore;
using ProductAttribute = CommerceHub.ProductCatalog.Domain.Attributes.Attribute;

namespace CommerceHub.ProductCatalog.Application.Attributes;

internal static class AttributeQueryExtensions
{
    public static IQueryable<ProductAttribute> WhereSearch(this IQueryable<ProductAttribute> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return query;
        }

        var term = search.Trim().ToLower();
        return query.Where(a => a.Name.ToLower().Contains(term) || a.Code.Contains(term));
    }

    public static Task<bool> CodeExistsAsync(
        this IQueryable<ProductAttribute> query,
        string code,
        long? excludeId,
        CancellationToken cancellationToken) =>
        query.AnyAsync(a => a.Code == code && (excludeId == null || a.Id != excludeId), cancellationToken);

    public static IQueryable<AttributeDto> ToDto(this IQueryable<ProductAttribute> query) =>
        query.Select(a => new AttributeDto(a.Id, a.Name, a.Code));

    public static AttributeDto ToDto(this ProductAttribute attribute) =>
        new(attribute.Id, attribute.Name, attribute.Code);
}
