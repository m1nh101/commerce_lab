using CommerceHub.ProductCatalog.Application.Common;

namespace CommerceHub.ProductCatalog.Application.Images;

public static class ImageErrors
{
    public static readonly Error NotFound =
        new("IMAGE_NOT_FOUND", "Image not found.", ErrorType.NotFound);

    public static readonly Error ProductNotFound =
        new("PRODUCT_NOT_FOUND", "Product not found.", ErrorType.NotFound);

    public static readonly Error VariantNotFound =
        new("VARIANT_NOT_FOUND", "Variant not found.", ErrorType.NotFound);

    public static readonly Error UnsupportedContentType =
        new("UNSUPPORTED_IMAGE_CONTENT_TYPE", "Only JPEG, PNG and WebP images are allowed.", ErrorType.Unprocessable);

    public static Error TooLarge(long maxContentLength) =>
        new("IMAGE_TOO_LARGE", $"'content_length' must be between 1 and {maxContentLength} bytes.", ErrorType.Unprocessable);
}
