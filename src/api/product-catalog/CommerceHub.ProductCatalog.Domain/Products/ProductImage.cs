using CommerceHub.ProductCatalog.Domain.Common;

namespace CommerceHub.ProductCatalog.Domain.Products;

public sealed class ProductImage : Entity<long>
{
    public const int UrlMaxLength = 2048;

    private ProductImage()
    {
    }

    public Guid ProductId { get; private set; }

    /// <summary>
    /// When <c>null</c>, the image applies to the product in general rather than a specific variant.
    /// </summary>
    public Guid? VariantId { get; private set; }

    public string Url { get; private set; } = null!;

    public int SortOrder { get; private set; }

    internal static ProductImage Create(Guid productId, string url, int sortOrder, Guid? variantId)
    {
        return new ProductImage
        {
            ProductId = productId,
            VariantId = variantId,
            Url = Guard.NotEmpty(url, UrlMaxLength, nameof(url)),
            SortOrder = sortOrder
        };
    }

    public void ChangeUrl(string url) => Url = Guard.NotEmpty(url, UrlMaxLength, nameof(url));

    public void ChangeSortOrder(int sortOrder) => SortOrder = sortOrder;
}
