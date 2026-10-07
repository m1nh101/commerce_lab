using CommerceHub.ProductCatalog.Application.Common;

namespace CommerceHub.ProductCatalog.Application.Products;

public static class ProductErrors
{
    public static readonly Error NotFound =
        new("PRODUCT_NOT_FOUND", "Product not found.", ErrorType.NotFound);

    public static readonly Error SlugConflict =
        new("PRODUCT_SLUG_ALREADY_EXISTS", "A product with the specified slug already exists.", ErrorType.Conflict);

    public static readonly Error CategoryNotFound =
        new("CATEGORY_NOT_FOUND", "One or more categories do not exist.", ErrorType.NotFound);
}
