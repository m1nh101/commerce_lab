using CommerceHub.ProductCatalog.Application.Common;

namespace CommerceHub.ProductCatalog.Application.Attributes;

public interface IAttributeService
{
    Task<Result<AttributeDto>> CreateAsync(CreateAttributeInput input, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<AttributeDto>>> ListAsync(AttributeListQuery query, CancellationToken cancellationToken = default);

    Task<Result<AttributeDto>> GetAsync(long id, CancellationToken cancellationToken = default);

    Task<Result<AttributeDto>> UpdateAsync(long id, UpdateAttributeInput input, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
