using CommerceHub.ProductCatalog.Domain.Common;

namespace CommerceHub.ProductCatalog.Domain.Products;

public sealed class Product : AuditableEntity<Guid>, IAggregateRoot
{
    public const int NameMaxLength = 255;
    public const int SlugMaxLength = 255;

    private readonly List<ProductVariant> _variants = [];
    private readonly List<ProductImage> _images = [];
    private readonly List<ProductCategory> _categories = [];

    private Product()
    {
    }

    public string Name { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public string? Description { get; private set; }

    public ProductStatus Status { get; private set; }

    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();

    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    public IReadOnlyCollection<ProductCategory> Categories => _categories.AsReadOnly();

    public static Product Create(string name, string slug, string? description = null)
    {
        return new Product
        {
            Id = Guid.CreateVersion7(),
            Name = Guard.NotEmpty(name, NameMaxLength, nameof(name)),
            Slug = Guard.NotEmpty(slug, SlugMaxLength, nameof(slug)),
            Description = description,
            Status = ProductStatus.Draft
        };
    }

    public void UpdateDetails(string name, string slug, string? description)
    {
        Name = Guard.NotEmpty(name, NameMaxLength, nameof(name));
        Slug = Guard.NotEmpty(slug, SlugMaxLength, nameof(slug));
        Description = description;
    }

    public void ChangeStatus(ProductStatus status) => Status = status;

    public ProductVariant AddVariant(string sku, string name, decimal price, string currency = ProductVariant.DefaultCurrency)
    {
        var normalizedSku = Guard.NotEmpty(sku, ProductVariant.SkuMaxLength, nameof(sku));
        if (_variants.Any(v => string.Equals(v.Sku, normalizedSku, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Variant with SKU '{normalizedSku}' already exists on this product.");
        }

        var variant = ProductVariant.Create(Id, normalizedSku, name, price, currency);
        _variants.Add(variant);
        return variant;
    }

    public void RemoveVariant(Guid variantId)
    {
        var variant = GetVariant(variantId);
        _images.RemoveAll(i => i.VariantId == variantId);
        _variants.Remove(variant);
    }

    public ProductVariant GetVariant(Guid variantId) =>
        _variants.SingleOrDefault(v => v.Id == variantId)
        ?? throw new InvalidOperationException($"Variant '{variantId}' does not belong to this product.");

    public ProductImage AddImage(string url, int sortOrder = 0, Guid? variantId = null)
    {
        if (variantId is { } id)
        {
            GetVariant(id);
        }

        var image = ProductImage.Create(Id, url, sortOrder, variantId);
        _images.Add(image);
        return image;
    }

    public void RemoveImage(ProductImage image) => _images.Remove(image);

    public void AssignCategory(long categoryId)
    {
        if (_categories.Any(c => c.CategoryId == categoryId))
        {
            return;
        }

        _categories.Add(new ProductCategory(Id, categoryId));
    }

    public void RemoveCategory(long categoryId) => _categories.RemoveAll(c => c.CategoryId == categoryId);
}
