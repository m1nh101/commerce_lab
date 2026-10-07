using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;

namespace CommerceHub.ProductCatalog.Application.Products.Queries;

public sealed record GetProductQuery(Guid Id, ProductIncludes Includes = ProductIncludes.None)
    : IQuery<Result<ProductDetailDto>>;

internal sealed class GetProductQueryHandler(IProductCatalogDbContext dbContext)
    : IQueryHandler<GetProductQuery, Result<ProductDetailDto>>
{
    public Task<Result<ProductDetailDto>> HandleAsync(GetProductQuery query, CancellationToken cancellationToken = default) =>
        dbContext.LoadProductDetailAsync(query.Id, query.Includes, cancellationToken);
}
