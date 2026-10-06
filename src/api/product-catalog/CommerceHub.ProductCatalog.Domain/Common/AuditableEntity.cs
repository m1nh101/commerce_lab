namespace CommerceHub.ProductCatalog.Domain.Common;

public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; }

    DateTimeOffset UpdatedAt { get; }

    void SetCreated(DateTimeOffset timestamp);

    void SetUpdated(DateTimeOffset timestamp);
}

public abstract class AuditableEntity<TId> : Entity<TId>, IAuditableEntity
    where TId : notnull
{
    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    void IAuditableEntity.SetCreated(DateTimeOffset timestamp)
    {
        CreatedAt = timestamp;
        UpdatedAt = timestamp;
    }

    void IAuditableEntity.SetUpdated(DateTimeOffset timestamp) => UpdatedAt = timestamp;
}
