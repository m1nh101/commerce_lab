using CommerceHub.ProductCatalog.Application.Common;
using CommerceHub.ProductCatalog.Domain.Products;

namespace CommerceHub.ProductCatalog.Application.Images;

/// <summary>
/// Pure input validation for image references and upload requests.
/// </summary>
internal static class ImageValidator
{
    // Allowed content types and the file extensions accepted for each; the first extension is used for the object key.
    private static readonly Dictionary<string, string[]> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = [".jpg", ".jpeg"],
        ["image/png"] = [".png"],
        ["image/webp"] = [".webp"]
    };

    public static Result<string> ValidateUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Error.Validation("'url' is required.");
        }

        var trimmed = url.Trim();
        if (trimmed.Length > ProductImage.UrlMaxLength)
        {
            return Error.Validation($"'url' must not exceed {ProductImage.UrlMaxLength} characters.");
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Error.Validation("'url' must be an absolute http(s) URL.");
        }

        return trimmed;
    }

    /// <summary>
    /// Validates an upload request and returns the normalized content type and file extension.
    /// </summary>
    public static Result<(string ContentType, string Extension)> ValidateUpload(
        string? fileName,
        string? contentType,
        long? contentLength,
        long maxContentLength)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Error.Validation("'file_name' is required.");
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            return Error.Validation("'content_type' is required.");
        }

        if (contentLength is null)
        {
            return Error.Validation("'content_length' is required.");
        }

        var normalizedType = contentType.Trim().ToLowerInvariant();
        if (!AllowedTypes.TryGetValue(normalizedType, out var extensions))
        {
            return ImageErrors.UnsupportedContentType;
        }

        var extension = Path.GetExtension(fileName.Trim()).ToLowerInvariant();
        if (!extensions.Contains(extension))
        {
            return ImageErrors.UnsupportedContentType;
        }

        if (contentLength <= 0 || contentLength > maxContentLength)
        {
            return ImageErrors.TooLarge(maxContentLength);
        }

        return (normalizedType, extensions[0]);
    }
}
