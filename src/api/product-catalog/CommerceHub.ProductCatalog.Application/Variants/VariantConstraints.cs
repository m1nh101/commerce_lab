namespace CommerceHub.ProductCatalog.Application.Variants;

/// <summary>
/// Database constraint names (see the migrations) used to map save-time violations to errors.
/// </summary>
internal static class VariantConstraints
{
    public const string SkuUniqueIndex = "ix_product_variants_sku";

    public const string AttributeForeignKey = "fk_product_variant_attributes_attributes_attribute_id";
}
