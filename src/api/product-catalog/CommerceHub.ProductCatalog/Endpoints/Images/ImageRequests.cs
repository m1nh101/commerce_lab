namespace CommerceHub.ProductCatalog.Endpoints.Images;

public sealed record CreateImageUploadUrlRequest(string? FileName, string? ContentType, long? ContentLength);

public sealed record AddImageRequest(string? Url, int? SortOrder);

public sealed record UpdateImageRequest(string? Url, int? SortOrder);
