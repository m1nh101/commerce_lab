using CommerceHub.ProductCatalog.Domain.Common;

namespace CommerceHub.ProductCatalog.Domain.Products;

/// <summary>
/// Value of an attribute (e.g. Size = "XL") for a variant. The attribute is a separate aggregate, so it is referenced by id only.
/// </summary>
public sealed class ProductVariantAttribute
{
    public const int ValueMaxLength = 255;

    private ProductVariantAttribute()
    {
    }

    internal ProductVariantAttribute(Guid variantId, long attributeId, string value)
    {
        VariantId = variantId;
        AttributeId = attributeId;
        Value = Guard.NotEmpty(value, ValueMaxLength, nameof(value));
    }

    public Guid VariantId { get; private set; }

    public long AttributeId { get; private set; }

    public string Value { get; private set; } = null!;

    internal void ChangeValue(string value) => Value = Guard.NotEmpty(value, ValueMaxLength, nameof(value));
}
