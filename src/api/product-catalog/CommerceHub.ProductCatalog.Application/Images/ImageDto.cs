using CommerceHub.ProductCatalog.Domain.Products;

namespace CommerceHub.ProductCatalog.Application.Images;

public sealed record ImageDto(long Id, Guid ProductId, Guid? VariantId, string Url, int SortOrder)
{
    internal static ImageDto From(ProductImage image) =>
        new(image.Id, image.ProductId, image.VariantId, image.Url, image.SortOrder);
}

public sealed record UploadUrlDto(
    string UploadUrl,
    string Method,
    IReadOnlyDictionary<string, string> Headers,
    string ObjectKey,
    string ImageUrl,
    DateTimeOffset ExpiresAt);
