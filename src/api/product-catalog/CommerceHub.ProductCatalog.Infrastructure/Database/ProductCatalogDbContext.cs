using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Domain.Categories;
using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ProductAttribute = CommerceHub.ProductCatalog.Domain.Attributes.Attribute;

namespace CommerceHub.ProductCatalog.Infrastructure.Database;

public sealed class ProductCatalogDbContext(DbContextOptions<ProductCatalogDbContext> options) : DbContext(options), IProductCatalogDbContext
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();

    public DbSet<ProductImage> ProductImages => Set<ProductImage>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<ProductAttribute> Attributes => Set<ProductAttribute>();

    public DbSet<ProductVariantAttribute> ProductVariantAttributes => Set<ProductVariantAttribute>();

    /// <summary>
    /// Translates Postgres unique/FK violations into <see cref="ConstraintViolationException"/> so the application layer
    /// can map them by constraint name without referencing Npgsql.
    /// </summary>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && ToViolationKind(pg.SqlState) is { } kind)
        {
            throw new ConstraintViolationException(kind, pg.ConstraintName, ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductCatalogDbContext).Assembly);
    }

    private static ConstraintViolationKind? ToViolationKind(string sqlState) => sqlState switch
    {
        PostgresErrorCodes.UniqueViolation => ConstraintViolationKind.Unique,
        PostgresErrorCodes.ForeignKeyViolation => ConstraintViolationKind.ForeignKey,
        _ => null,
    };
}
