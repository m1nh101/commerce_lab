using CommerceHub.Cqrs;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;

namespace CommerceHub.ProductCatalog.Application.Variants.Queries;

public sealed record GetVariantQuery(Guid Id) : IQuery<Result<VariantDto>>;

internal sealed class GetVariantQueryHandler(IProductCatalogDbContext dbContext)
    : IQueryHandler<GetVariantQuery, Result<VariantDto>>
{
    public Task<Result<VariantDto>> HandleAsync(GetVariantQuery query, CancellationToken cancellationToken = default) =>
        dbContext.LoadVariantDtoAsync(query.Id, cancellationToken);
}
