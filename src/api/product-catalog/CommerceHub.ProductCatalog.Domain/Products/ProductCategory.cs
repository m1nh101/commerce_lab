namespace CommerceHub.ProductCatalog.Domain.Products;

/// <summary>
/// Links a product to a category. The category is a separate aggregate, so it is referenced by id only.
/// </summary>
public sealed class ProductCategory
{
    private ProductCategory()
    {
    }

    internal ProductCategory(Guid productId, long categoryId)
    {
        ProductId = productId;
        CategoryId = categoryId;
    }

    public Guid ProductId { get; private set; }

    public long CategoryId { get; private set; }
}
