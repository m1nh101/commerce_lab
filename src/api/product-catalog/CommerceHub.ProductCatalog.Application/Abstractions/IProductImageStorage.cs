namespace CommerceHub.ProductCatalog.Application.Abstractions;

/// <summary>
/// A presigned, single-object upload target in the dedicated product-image bucket.
/// </summary>
public sealed record PresignedUpload(
    string UploadUrl,
    string Method,
    IReadOnlyDictionary<string, string> Headers,
    string ObjectKey,
    string ImageUrl,
    DateTimeOffset ExpiresAt);

/// <summary>
/// Issues upload URLs for product images. The bucket and object key are always chosen by the implementation, never the
/// caller.
/// </summary>
public interface IProductImageStorage
{
    PresignedUpload CreateUploadUrl(Guid productId, string extension, string contentType, long contentLength);
}
