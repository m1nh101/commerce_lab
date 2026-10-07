namespace CommerceHub.ProductCatalog.Application.Attributes;

public sealed record CreateAttributeInput(string? Name, string? Code);

public sealed record UpdateAttributeInput(string? Name, string? Code);

public sealed record AttributeListQuery(string? Search, int Page, int Limit);
