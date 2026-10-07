using Microsoft.EntityFrameworkCore;

namespace CommerceHub.ProductCatalog.Application.Abstractions;

public enum ConstraintViolationKind
{
    Unique,
    ForeignKey,
}

/// <summary>
/// Raised by the persistence layer when a save violates a database constraint, so the application can map it
/// to a business error by constraint name without depending on the database provider.
/// Derives from <see cref="DbUpdateException"/> so existing handlers keep working.
/// </summary>
public sealed class ConstraintViolationException(ConstraintViolationKind kind, string? constraintName, DbUpdateException innerException)
    : DbUpdateException($"Database {kind} constraint '{constraintName}' was violated.", innerException, innerException.Entries)
{
    public ConstraintViolationKind Kind { get; } = kind;

    public string? ConstraintName { get; } = constraintName;
}
