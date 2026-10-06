using CommerceHub.ProductCatalog.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceHub.ProductCatalog.Infrastructure.Database.Configurations;

internal static class AuditColumnsConfiguration
{
    /// <summary>
    /// Maps the <see cref="AuditableEntity{TId}"/> columns as UTC <c>timestamptz</c> defaulting to the current time.
    /// </summary>
    public static void ConfigureAuditColumns<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditableEntity
    {
        builder.Property(e => e.CreatedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();
    }
}
