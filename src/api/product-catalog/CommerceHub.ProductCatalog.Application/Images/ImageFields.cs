namespace CommerceHub.ProductCatalog.Application.Images;

/// <summary>
/// Raw image reference input as received from the API.
/// </summary>
public sealed record ImageFields(string? Url, int? SortOrder);
