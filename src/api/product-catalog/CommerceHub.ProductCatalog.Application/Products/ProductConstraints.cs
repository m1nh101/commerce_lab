namespace CommerceHub.ProductCatalog.Application.Products;

/// <summary>
/// Database constraint names (see the migrations) used to map save-time violations to errors.
/// </summary>
internal static class ProductConstraints
{
    public const string SlugUniqueIndex = "ix_products_slug";

    public const string CategoryForeignKey = "fk_product_categories_categories_category_id";
}
