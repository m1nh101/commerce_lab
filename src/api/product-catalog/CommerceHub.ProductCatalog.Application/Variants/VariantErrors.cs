using CommerceHub.ProductCatalog.Application.Common;

namespace CommerceHub.ProductCatalog.Application.Variants;

public static class VariantErrors
{
    public static readonly Error NotFound =
        new("VARIANT_NOT_FOUND", "Variant not found.", ErrorType.NotFound);

    public static readonly Error ProductNotFound =
        new("PRODUCT_NOT_FOUND", "Product not found.", ErrorType.NotFound);

    public static readonly Error SkuConflict =
        new("VARIANT_SKU_ALREADY_EXISTS", "A variant with this SKU already exists.", ErrorType.Conflict);

    public static readonly Error AttributeNotAssigned =
        new("VARIANT_ATTRIBUTE_NOT_FOUND", "The variant has no value for this attribute.", ErrorType.NotFound);
}
