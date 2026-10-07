using CommerceHub.ProductCatalog.Domain.Common;

namespace CommerceHub.ProductCatalog.Domain.Products;

public sealed class ProductVariant : AuditableEntity<Guid>
{
    public const int SkuMaxLength = 100;
    public const int NameMaxLength = 255;
    public const int CurrencyLength = 3;
    public const string DefaultCurrency = "USD";

    private readonly List<ProductVariantAttribute> _attributes = [];

    private ProductVariant()
    {
    }

    public Guid ProductId { get; private set; }

    public string Sku { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public decimal Price { get; private set; }

    public string Currency { get; private set; } = DefaultCurrency;

    public ProductVariantStatus Status { get; private set; }

    public IReadOnlyCollection<ProductVariantAttribute> Attributes => _attributes.AsReadOnly();

    internal static ProductVariant Create(Guid productId, string sku, string name, decimal price, string currency)
    {
        var variant = new ProductVariant
        {
            Id = Guid.CreateVersion7(),
            ProductId = productId,
            Sku = Guard.NotEmpty(sku, SkuMaxLength, nameof(sku)),
            Name = Guard.NotEmpty(name, NameMaxLength, nameof(name)),
            Status = ProductVariantStatus.Draft
        };
        variant.ChangePrice(price, currency);
        return variant;
    }

    public void ChangeSku(string sku) => Sku = Guard.NotEmpty(sku, SkuMaxLength, nameof(sku));

    public void Rename(string name) => Name = Guard.NotEmpty(name, NameMaxLength, nameof(name));

    public void ChangePrice(decimal price, string currency)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        if (currency.Length != CurrencyLength)
        {
            throw new ArgumentException("Currency must be a 3-letter ISO 4217 code.", nameof(currency));
        }

        Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    public void ChangeStatus(ProductVariantStatus status) => Status = status;

    /// <summary>
    /// Sets the value for an attribute, replacing any existing value so each attribute appears once per variant.
    /// </summary>
    public void SetAttribute(long attributeId, string value)
    {
        var existing = _attributes.SingleOrDefault(a => a.AttributeId == attributeId);
        if (existing is not null)
        {
            existing.ChangeValue(value);
            return;
        }

        _attributes.Add(new ProductVariantAttribute(Id, attributeId, value));
    }

    public void RemoveAttribute(long attributeId) => _attributes.RemoveAll(a => a.AttributeId == attributeId);
}
