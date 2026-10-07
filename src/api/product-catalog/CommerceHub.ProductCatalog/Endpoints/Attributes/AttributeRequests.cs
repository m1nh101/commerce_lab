namespace CommerceHub.ProductCatalog.Endpoints.Attributes;

public sealed record CreateAttributeRequest(string? Name, string? Code);

public sealed record UpdateAttributeRequest(string? Name, string? Code);
