using Amazon.S3;
using Amazon.S3.Model;
using CommerceHub.ProductCatalog.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace CommerceHub.ProductCatalog.Infrastructure.Storage;

internal sealed class S3ProductImageStorage(
    IAmazonS3 s3,
    IOptions<ProductImageStorageOptions> options,
    TimeProvider timeProvider) : IProductImageStorage
{
    public PresignedUpload CreateUploadUrl(Guid productId, string extension, string contentType, long contentLength)
    {
        var settings = options.Value;
        // The key is always server-generated; the client's file name only contributes the (validated) extension.
        var objectKey = $"products/{productId}/{Guid.CreateVersion7()}{extension}";
        var expiresAt = timeProvider.GetUtcNow().Add(settings.UploadUrlExpiry);

        // S3 presigned PUTs cannot enforce Content-Length, so size is only checked when the URL is issued.
        var uploadUrl = s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = settings.BucketName,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = expiresAt.UtcDateTime,
            Protocol = settings.ServiceUrl?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) == true
                ? Protocol.HTTP
                : Protocol.HTTPS
        });

        return new PresignedUpload(
            uploadUrl,
            "PUT",
            new Dictionary<string, string> { ["Content-Type"] = contentType },
            objectKey,
            $"{PublicBaseUrl(settings)}/{objectKey}",
            expiresAt);
    }

    private static string PublicBaseUrl(ProductImageStorageOptions settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.PublicBaseUrl))
        {
            return settings.PublicBaseUrl.TrimEnd('/');
        }

        if (!string.IsNullOrWhiteSpace(settings.ServiceUrl))
        {
            return $"{settings.ServiceUrl.TrimEnd('/')}/{settings.BucketName}";
        }

        return $"https://{settings.BucketName}.s3.{settings.Region}.amazonaws.com";
    }
}
