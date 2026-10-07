namespace CommerceHub.ProductCatalog.Application.Images;

public sealed class ImageUploadOptions
{
    public const string SectionName = "ProductImages";

    /// <summary>
    /// Maximum accepted upload size in bytes. Defaults to 10 MB.
    /// </summary>
    public long MaxContentLength { get; set; } = 10 * 1024 * 1024;
}
