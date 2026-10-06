using CommerceHub.ProductCatalog.Domain.Common;

namespace CommerceHub.ProductCatalog.Domain.Categories;

public sealed class Category : Entity<long>, IAggregateRoot
{
    public const int NameMaxLength = 255;
    public const int SlugMaxLength = 255;

    private Category()
    {
    }

    /// <summary>
    /// Parent category id; <c>null</c> denotes a root category.
    /// </summary>
    public long? ParentId { get; private set; }

    public string Name { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public CategoryStatus Status { get; private set; }

    public static Category Create(string name, string slug, long? parentId = null)
    {
        return new Category
        {
            Name = Guard.NotEmpty(name, NameMaxLength, nameof(name)),
            Slug = Guard.NotEmpty(slug, SlugMaxLength, nameof(slug)),
            ParentId = parentId,
            Status = CategoryStatus.Active
        };
    }

    public void UpdateDetails(string name, string slug)
    {
        Name = Guard.NotEmpty(name, NameMaxLength, nameof(name));
        Slug = Guard.NotEmpty(slug, SlugMaxLength, nameof(slug));
    }

    /// <summary>
    /// Moves the category under a new parent. Checking for descendant cycles needs the whole tree, so it belongs in a domain service.
    /// </summary>
    public void MoveTo(long? parentId)
    {
        if (parentId is not null && parentId == Id)
        {
            throw new InvalidOperationException("A category cannot be its own parent.");
        }

        ParentId = parentId;
    }

    public void ChangeStatus(CategoryStatus status) => Status = status;
}
