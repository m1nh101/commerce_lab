using System.Text.RegularExpressions;
using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Application.Common;
using CommerceHub.ProductCatalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Categories;

internal sealed partial class CategoryService(IProductCatalogDbContext dbContext) : ICategoryService
{
    private const int MaxPageSize = 100;

    private static readonly Error InvalidStatus =
        Error.Validation("'status' must be one of: active, hidden, archived.");

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryInput input, CancellationToken cancellationToken = default)
    {
        if (ValidateName(input.Name) is { } nameError)
        {
            return nameError;
        }

        var name = input.Name!.Trim();
        var slug = string.IsNullOrWhiteSpace(input.Slug) ? SlugGenerator.Generate(name) : input.Slug.Trim();
        if (ValidateSlug(slug) is { } slugError)
        {
            return slugError;
        }

        var status = input.Status ?? CategoryStatus.Active;
        if (!Enum.IsDefined(status))
        {
            return InvalidStatus;
        }

        if (await dbContext.Categories.SlugExistsAsync(slug, excludeId: null, cancellationToken))
        {
            return CategoryErrors.SlugConflict;
        }

        if (input.ParentId is { } parentId &&
            !await dbContext.Categories.AnyAsync(c => c.Id == parentId, cancellationToken))
        {
            return CategoryErrors.ParentNotFound;
        }

        var category = Category.Create(name, slug, input.ParentId);
        category.ChangeStatus(status);

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return category.ToDto();
    }

    public async Task<Result<PagedResult<CategoryDto>>> ListAsync(CategoryListQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Page < 1)
        {
            return Error.Validation("'page' must be greater than or equal to 1.");
        }

        if (query.Limit is < 1 or > MaxPageSize)
        {
            return Error.Validation($"'limit' must be between 1 and {MaxPageSize}.");
        }

        var filtered = Filter(query);
        var total = await filtered.CountAsync(cancellationToken);
        var items = await filtered
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((query.Page - 1) * query.Limit)
            .Take(query.Limit)
            .ToDto()
            .ToListAsync(cancellationToken);

        return new PagedResult<CategoryDto>(items, total, query.Page, query.Limit);
    }

    public async Task<IReadOnlyList<CategoryTreeNodeDto>> GetTreeAsync(CategoryListQuery query, CancellationToken cancellationToken = default)
    {
        var categories = await Filter(query)
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .ToDto()
            .ToListAsync(cancellationToken);

        var nodes = categories.ToDictionary(
            c => c.Id,
            c => new CategoryTreeNodeDto(c.Id, c.ParentId, c.Name, c.Slug, c.Status, []));

        var roots = new List<CategoryTreeNodeDto>();
        foreach (var category in categories)
        {
            var node = nodes[category.Id];

            // Categories whose parent was filtered out are promoted to roots so they are not lost.
            if (category.ParentId is { } parentId && nodes.TryGetValue(parentId, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        return roots;
    }

    public async Task<Result<CategoryDto>> GetAsync(string idOrSlug, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories
            .AsNoTracking()
            .WhereIdOrSlug(idOrSlug.Trim())
            .ToDto()
            .FirstOrDefaultAsync(cancellationToken);

        return category is null ? CategoryErrors.NotFound : (Result<CategoryDto>)category;
    }

    public async Task<Result<CategoryDto>> UpdateAsync(long id, UpdateCategoryInput input, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return CategoryErrors.NotFound;
        }

        if (input.Name is not null && ValidateName(input.Name) is { } nameError)
        {
            return nameError;
        }

        var slug = input.Slug?.Trim();
        if (slug is not null && ValidateSlug(slug) is { } slugError)
        {
            return slugError;
        }

        if (input.Status is { } status && !Enum.IsDefined(status))
        {
            return InvalidStatus;
        }

        if (slug is not null && slug != category.Slug &&
            await dbContext.Categories.SlugExistsAsync(slug, id, cancellationToken))
        {
            return CategoryErrors.SlugConflict;
        }

        if (input.ParentIdSpecified && input.ParentId != category.ParentId && input.ParentId is { } parentId)
        {
            if (parentId == id)
            {
                return CategoryErrors.CircularParent;
            }

            var parentMap = await dbContext.Categories.GetParentMapAsync(cancellationToken);
            if (!parentMap.ContainsKey(parentId))
            {
                return CategoryErrors.ParentNotFound;
            }

            if (IsSelfOrDescendant(parentMap, parentId, id))
            {
                return CategoryErrors.CircularParent;
            }
        }

        category.UpdateDetails(input.Name ?? category.Name, slug ?? category.Slug);

        if (input.ParentIdSpecified)
        {
            category.MoveTo(input.ParentId);
        }

        if (input.Status is { } newStatus)
        {
            category.ChangeStatus(newStatus);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return category.ToDto();
    }

    public async Task<Result> DeleteAsync(long id, long? reassignChildrenTo, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return CategoryErrors.NotFound;
        }

        var children = await dbContext.Categories
            .Where(c => c.ParentId == id)
            .ToListAsync(cancellationToken);

        if (children.Count > 0)
        {
            if (reassignChildrenTo is not { } targetId)
            {
                return CategoryErrors.HasChildren;
            }

            var parentMap = await dbContext.Categories.GetParentMapAsync(cancellationToken);
            if (!parentMap.ContainsKey(targetId) || IsSelfOrDescendant(parentMap, targetId, id))
            {
                return CategoryErrors.ReassignTargetInvalid;
            }

            foreach (var child in children)
            {
                child.MoveTo(targetId);
            }
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private IQueryable<Category> Filter(CategoryListQuery query) =>
        dbContext.Categories
            .AsNoTracking()
            .WhereStatus(query.Status)
            .WhereSearch(query.Search)
            .WhereParent(query.ParentIdSpecified, query.ParentId);

    /// <summary>
    /// Walks up from <paramref name="candidateId"/> and returns <c>true</c> if <paramref name="ancestorId"/> is reached.
    /// </summary>
    private static bool IsSelfOrDescendant(Dictionary<long, long?> parentMap, long candidateId, long ancestorId)
    {
        var visited = new HashSet<long>();
        long? current = candidateId;

        while (current is { } currentId && visited.Add(currentId))
        {
            if (currentId == ancestorId)
            {
                return true;
            }

            current = parentMap.GetValueOrDefault(currentId);
        }

        return false;
    }

    private static Error? ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("'name' is required.");
        }

        return name.Trim().Length > Category.NameMaxLength
            ? Error.Validation($"'name' must not exceed {Category.NameMaxLength} characters.")
            : null;
    }

    private static Error? ValidateSlug(string slug)
    {
        if (slug.Length > Category.SlugMaxLength)
        {
            return Error.Validation($"'slug' must not exceed {Category.SlugMaxLength} characters.");
        }

        return SlugPattern().IsMatch(slug)
            ? null
            : Error.Validation("'slug' must be lowercase alphanumeric words separated by single hyphens.");
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
