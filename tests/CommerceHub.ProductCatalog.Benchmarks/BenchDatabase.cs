using CommerceHub.ProductCatalog.Domain.Categories;
using CommerceHub.ProductCatalog.Domain.Products;
using CommerceHub.ProductCatalog.Infrastructure.Database;
using CommerceHub.ProductCatalog.Infrastructure.Database.Interceptors;
using Microsoft.EntityFrameworkCore;
using ProductAttribute = CommerceHub.ProductCatalog.Domain.Attributes.Attribute;

namespace CommerceHub.ProductCatalog.Benchmarks;

/// <summary>
/// A dedicated Postgres database (production mappings + migrations) seeded once with a deterministic dataset.
/// Connection string: env <c>ConnectionStrings__ProductCatalogBench</c>, defaulting to the docker compose Postgres.
/// </summary>
internal static class BenchDatabase
{
    public const int AttributeCount = 30;
    public const int ProductCount = 10_000;
    public const int VariantsPerProduct = 3;

    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=product_catalog_bench;Username=postgres;Password=postgres";

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static BenchData? _data;

    public static string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("ConnectionStrings__ProductCatalogBench") ?? DefaultConnectionString;

    public static DbContextOptions<ProductCatalogDbContext> CreateOptions(Action<DbContextOptionsBuilder>? configure = null)
    {
        var builder = new DbContextOptionsBuilder<ProductCatalogDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new AuditableEntityInterceptor(TimeProvider.System));
        configure?.Invoke(builder);
        return builder.Options;
    }

    public static ProductCatalogDbContext NewContext() => new(CreateOptions());

    /// <summary>Migrates and seeds (only when empty), then returns the ids/slugs that benchmarks look up.</summary>
    public static async Task<BenchData> EnsureSeededAsync()
    {
        if (_data is not null)
        {
            return _data;
        }

        await Gate.WaitAsync();
        try
        {
            if (_data is not null)
            {
                return _data;
            }

            await using (var ctx = NewContext())
            {
                await ctx.Database.MigrateAsync();
                if (!await ctx.Products.AnyAsync())
                {
                    await SeedAsync();
                }
            }

            return _data = await LoadAsync();
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task SeedAsync()
    {
        var random = new Random(42);

        // 3-level category tree: 5 roots, 15 children, 30 grandchildren.
        var categoryIds = new List<long>();
        var parents = new List<long?> { null };
        foreach (var levelSize in new[] { 5, 15, 30 })
        {
            await using var ctx = NewContext();
            var start = categoryIds.Count;
            var level = Enumerable.Range(start + 1, levelSize)
                .Select(n => Category.Create($"Category {n}", $"category-{n}", parents[n % parents.Count]))
                .ToList();
            ctx.Categories.AddRange(level);
            await ctx.SaveChangesAsync();
            parents = level.Select(c => (long?)c.Id).ToList();
            categoryIds.AddRange(level.Select(c => c.Id));
        }

        List<long> attributeIds;
        await using (var ctx = NewContext())
        {
            var attributes = Enumerable.Range(1, AttributeCount)
                .Select(i => ProductAttribute.Create($"Attribute {i}", $"attr_{i}"))
                .ToList();
            ctx.Attributes.AddRange(attributes);
            await ctx.SaveChangesAsync();
            attributeIds = attributes.Select(a => a.Id).ToList();
        }

        var statuses = Enum.GetValues<ProductStatus>();
        const int batchSize = 1_000;
        for (var offset = 0; offset < ProductCount; offset += batchSize)
        {
            await using var ctx = NewContext();
            for (var n = offset + 1; n <= offset + batchSize; n++)
            {
                var product = Product.Create($"Product {n:D5}", $"product-{n:D5}", $"Description for product {n}.");
                product.ChangeStatus(statuses[random.Next(statuses.Length)]);
                product.AssignCategory(categoryIds[random.Next(categoryIds.Count)]);
                product.AssignCategory(categoryIds[random.Next(categoryIds.Count)]);
                for (var v = 1; v <= VariantsPerProduct; v++)
                {
                    var variant = product.AddVariant($"SKU-{n:D5}-{v}", $"Variant {v}", random.Next(100, 10_000) / 10m);
                    variant.SetAttribute(attributeIds[random.Next(attributeIds.Count)], $"value-{v}");
                    product.AddImage($"https://cdn.example.com/products/{n}/{v}.jpg", v, variant.Id);
                }

                product.AddImage($"https://cdn.example.com/products/{n}/main.jpg");
                ctx.Products.Add(product);
            }

            await ctx.SaveChangesAsync();
        }
    }

    private static async Task<BenchData> LoadAsync()
    {
        await using var ctx = NewContext();
        var product = await ctx.Products.AsNoTracking().OrderBy(p => p.Slug)
            .Skip(ProductCount / 2).Select(p => new { p.Id, p.Slug }).FirstAsync();
        var variantId = await ctx.ProductVariants.AsNoTracking().Where(v => v.ProductId == product.Id)
            .Select(v => v.Id).FirstAsync();
        var category = await ctx.Categories.AsNoTracking().OrderByDescending(c => c.Id)
            .Select(c => new { c.Id, c.Slug }).FirstAsync();
        var attributeId = await ctx.Attributes.AsNoTracking().OrderBy(a => a.Id).Select(a => a.Id).FirstAsync();
        var busiestCategoryId = await ctx.Products.AsNoTracking().SelectMany(p => p.Categories)
            .GroupBy(c => c.CategoryId).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstAsync();

        return new BenchData(product.Id, product.Slug, variantId, category.Id, category.Slug, attributeId, busiestCategoryId);
    }
}

internal sealed record BenchData(
    Guid ProductId,
    string ProductSlug,
    Guid VariantId,
    long CategoryId,
    string CategorySlug,
    long AttributeId,
    long BusiestCategoryId);
