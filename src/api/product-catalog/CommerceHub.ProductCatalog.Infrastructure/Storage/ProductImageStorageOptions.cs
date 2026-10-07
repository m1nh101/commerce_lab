using System.ComponentModel.DataAnnotations;

namespace CommerceHub.ProductCatalog.Infrastructure.Storage;

/// <summary>
/// S3 settings for the dedicated, public-read product-image bucket. Nothing other than product images may be stored in it.
/// </summary>
public sealed class ProductImageStorageOptions
{
    public const string SectionName = "ProductImages:Storage";

    [Required]
    public string BucketName { get; set; } = null!;

    [Required]
    public string Region { get; set; } = null!;

    /// <summary>
    /// Optional custom endpoint (e.g. LocalStack at <c>http://localhost:4566</c>). When unset, AWS S3 is used.
    /// </summary>
    public string? ServiceUrl { get; set; }

    public bool ForcePathStyle { get; set; }

    /// <summary>
    /// Optional base URL images are served from (e.g. a CDN). Defaults to the bucket's public URL.
    /// </summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>
    /// Optional static credentials (for LocalStack). When unset, the default AWS credential chain is used.
    /// </summary>
    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    [Range(typeof(TimeSpan), "00:01:00", "01:00:00")]
    public TimeSpan UploadUrlExpiry { get; set; } = TimeSpan.FromMinutes(15);
}
