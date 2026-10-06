using CommerceHub.ProductCatalog.Application.Common;

namespace CommerceHub.ProductCatalog.Application.Categories;

public static class CategoryErrors
{
    public static readonly Error NotFound =
        new("CATEGORY_NOT_FOUND", "Category not found.", ErrorType.NotFound);

    public static readonly Error SlugConflict =
        new("SLUG_ALREADY_EXISTS", "A category with this slug already exists.", ErrorType.Conflict);

    public static readonly Error ParentNotFound =
        new("PARENT_NOT_FOUND", "Parent category does not exist.", ErrorType.Validation);

    public static readonly Error CircularParent =
        new("CIRCULAR_PARENT", "A category cannot be its own parent or a child of its descendants.", ErrorType.Validation);

    public static readonly Error HasChildren =
        new(
            "HAS_CHILDREN",
            "Cannot delete category with active subcategories. Provide 'reassign_children_to' parameter or delete child categories first.",
            ErrorType.Conflict);

    public static readonly Error ReassignTargetInvalid =
        new(
            "INVALID_REASSIGN_TARGET",
            "'reassign_children_to' must be an existing category that is neither the deleted category nor one of its descendants.",
            ErrorType.Validation);
}
