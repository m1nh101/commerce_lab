using CommerceHub.ProductCatalog.Application.Common;

namespace CommerceHub.ProductCatalog.Application.Categories;

public interface ICategoryService
{
    Task<Result<CategoryDto>> CreateAsync(CreateCategoryInput input, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<CategoryDto>>> ListAsync(CategoryListQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryTreeNodeDto>> GetTreeAsync(CategoryListQuery query, CancellationToken cancellationToken = default);

    Task<Result<CategoryDto>> GetAsync(string idOrSlug, CancellationToken cancellationToken = default);

    Task<Result<CategoryDto>> UpdateAsync(long id, UpdateCategoryInput input, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(long id, long? reassignChildrenTo, CancellationToken cancellationToken = default);
}
