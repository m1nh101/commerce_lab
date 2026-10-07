using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Products.Queries;

/// <summary>
/// <see cref="Search"/> is a case-insensitive substring match on name or slug; <see cref="Slug"/> is an exact match.
/// </summary>
public sealed record ListProductsQuery(
    ProductStatus? Status,
    long? CategoryId,
    string? Search,
    string? Slug,
    ProductSort Sort = ProductSort.CreatedAtDesc,
    int Page = 1,
    int Limit = 20) : IQuery<Result<PagedResult<ProductDto>>>;

internal sealed class ListProductsQueryHandler(IProductCatalogDbContext dbContext)
    : IQueryHandler<ListProductsQuery, Result<PagedResult<ProductDto>>>
{
    private const int MaxPageSize = 100;

    public async Task<Result<PagedResult<ProductDto>>> HandleAsync(ListProductsQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Page < 1)
        {
            return Error.Validation("'page' must be greater than or equal to 1.");
        }

        if (query.Limit is < 1 or > MaxPageSize)
        {
            return Error.Validation($"'limit' must be between 1 and {MaxPageSize}.");
        }

        var filtered = dbContext.Products
            .AsNoTracking()
            .WhereStatus(query.Status)
            .WhereCategory(query.CategoryId)
            .WhereSearch(query.Search)
            .WhereSlug(query.Slug);

        var total = await filtered.CountAsync(cancellationToken);
        var items = await filtered
            .ApplySort(query.Sort)
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToDto()
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>(items, total, query.Page, query.Limit);
    }
}
