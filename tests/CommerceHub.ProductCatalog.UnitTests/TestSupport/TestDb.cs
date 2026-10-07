using CommerceHub.ProductCatalog.Application.Abstractions;
using CommerceHub.ProductCatalog.Domain.Categories;
using CommerceHub.ProductCatalog.Domain.Products;
using CommerceHub.ProductCatalog.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using ProductAttribute = CommerceHub.ProductCatalog.Domain.Attributes.Attribute;

namespace CommerceHub.ProductCatalog.UnitTests.TestSupport;

/// <summary>
/// The production <see cref="ProductCatalogDbContext"/> (same entity mappings) on the EF InMemory provider.
/// Each instance owns an isolated database; <see cref="NewContext"/> opens a fresh context on it to assert persisted state.
/// </summary>
internal sealed class TestDb : IProductCatalogDbContext, IDisposable
{
    private readonly string _name = Guid.NewGuid().ToString();
    private readonly ProductCatalogDbContext _context;

    public TestDb()
    {
        _context = NewContext();
    }

    /// <summary>When set, the next <see cref="SaveChangesAsync"/> throws it instead of saving (simulates DB constraints).</summary>
    public Exception? FailNextSaveWith { get; set; }

    public DbSet<Product> Products => _context.Products;

    public DbSet<ProductVariant> ProductVariants => _context.ProductVariants;

    public DbSet<ProductImage> ProductImages => _context.ProductImages;

    public DbSet<Category> Categories => _context.Categories;

    public DbSet<ProductAttribute> Attributes => _context.Attributes;

    public DbSet<ProductVariantAttribute> ProductVariantAttributes => _context.ProductVariantAttributes;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (FailNextSaveWith is { } ex)
        {
            FailNextSaveWith = null;
            throw ex;
        }

        return _context.SaveChangesAsync(cancellationToken);
    }

    public ProductCatalogDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ProductCatalogDbContext>().UseInMemoryDatabase(_name).Options);

    public async Task<Product> SeedProductAsync(
        string name = "Men's Casual T-Shirt",
        string slug = "mens-casual-t-shirt",
        string? description = "Cotton casual T-shirt.",
        ProductStatus status = ProductStatus.Draft,
        params long[] categoryIds)
    {
        await using var ctx = NewContext();
        var product = Product.Create(name, slug, description);
        product.ChangeStatus(status);
        foreach (var id in categoryIds)
        {
            product.AssignCategory(id);
        }

        ctx.Products.Add(product);
        await ctx.SaveChangesAsync();
        return product;
    }

    public async Task<Category> SeedCategoryAsync(string name, string slug)
    {
        await using var ctx = NewContext();
        var category = Category.Create(name, slug);
        ctx.Categories.Add(category);
        await ctx.SaveChangesAsync();
        return category;
    }

    public async Task<Product?> FindProductAsync(Guid id)
    {
        await using var ctx = NewContext();
        return await ctx.Products.AsNoTracking().Include(p => p.Categories).Include(p => p.Variants)
            .SingleOrDefaultAsync(p => p.Id == id);
    }

    public static ConstraintViolationException UniqueViolation(string constraint) =>
        new(ConstraintViolationKind.Unique, constraint, new DbUpdateException("duplicate key"));

    public void Dispose() => _context.Dispose();
}
