using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Domain.Categories;
using CommerceHub.ProductCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using ProductAttribute = CommerceHub.ProductCatalog.Domain.Attributes.Attribute;

namespace CommerceHub.ProductCatalog.Infrastructure.Database;

public sealed class ProductCatalogDbContext(DbContextOptions<ProductCatalogDbContext> options) : DbContext(options), IProductCatalogDbContext
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<ProductAttribute> Attributes => Set<ProductAttribute>();

    public DbSet<ProductVariantAttribute> ProductVariantAttributes => Set<ProductVariantAttribute>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductCatalogDbContext).Assembly);
    }
}
