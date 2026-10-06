namespace CommerceHub.ProductCatalog.Domain.Common;

/// <summary>
/// Marks an entity as the root of an aggregate. Only aggregate roots are loaded and persisted directly.
/// </summary>
public interface IAggregateRoot;
