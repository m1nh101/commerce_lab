using CommerceHub.ProductCatalog.Application.Attributes;
using CommerceHub.ProductCatalog.Application.Categories;
using CommerceHub.ProductCatalog.Application.Products;
using CommerceHub.ProductCatalog.Application.Products.Queries;
using CommerceHub.ProductCatalog.Application.Variants.Queries;
using CommerceHub.ProductCatalog.Domain.Products;
using CommerceHub.ProductCatalog.Infrastructure.Database;

namespace CommerceHub.ProductCatalog.Benchmarks;

/// <summary>
/// Every read query in product-catalog, exercised through its real handler/service.
/// Shared by the BenchmarkDotNet classes and <see cref="QueryProfiler"/> so both measure the same calls.
/// </summary>
internal static class BenchQueries
{
    private const ProductIncludes AllIncludes = ProductIncludes.Variants | ProductIncludes.Categories;

    public static readonly IReadOnlyList<(string Name, Func<ProductCatalogDbContext, BenchData, Task<object>> Run)> All =
    [
        ("GetProduct (no includes)", async (db, d) => await GetProduct(db, d.ProductId, ProductIncludes.None)),
        ("GetProduct (all includes)", async (db, d) => await GetProduct(db, d.ProductId, AllIncludes)),
        ("GetProduct (not found)", async (db, _) => await GetProduct(db, Guid.Empty, AllIncludes)),
        ("ListProducts (default)", async (db, _) => await ListProducts(db, new ListProductsQuery(null, null, null, null))),
        ("ListProducts (limit 100)", async (db, _) => await ListProducts(db, new ListProductsQuery(null, null, null, null, Limit: 100))),
        ("ListProducts (deep page)", async (db, _) => await ListProducts(db, new ListProductsQuery(null, null, null, null, Page: 400))),
        ("ListProducts (status)", async (db, _) => await ListProducts(db, new ListProductsQuery(ProductStatus.Active, null, null, null))),
        ("ListProducts (category)", async (db, d) => await ListProducts(db, new ListProductsQuery(null, d.BusiestCategoryId, null, null))),
        ("ListProducts (search)", async (db, _) => await ListProducts(db, new ListProductsQuery(null, null, "duct 05", null, ProductSort.NameAsc))),
        ("ListProducts (slug)", async (db, d) => await ListProducts(db, new ListProductsQuery(null, null, null, d.ProductSlug))),
        ("GetVariant", async (db, d) => await GetVariant(db, d.VariantId)),
        ("GetVariant (not found)", async (db, _) => await GetVariant(db, Guid.Empty)),
        ("ListCategories", async (db, _) => await Categories(db).ListAsync(new CategoryListQuery(false, null, null, null))),
        ("GetCategoryTree", async (db, _) => await Categories(db).GetTreeAsync(new CategoryListQuery(false, null, null, null))),
        ("GetCategory (id)", async (db, d) => await Categories(db).GetAsync(d.CategoryId.ToString())),
        ("GetCategory (slug)", async (db, d) => await Categories(db).GetAsync(d.CategorySlug)),
        ("ListAttributes", async (db, _) => await Attributes(db).ListAsync(new AttributeListQuery(null, 1, 20))),
        ("GetAttribute", async (db, d) => await Attributes(db).GetAsync(d.AttributeId)),
    ];

    public static Task<object> GetProduct(ProductCatalogDbContext db, Guid id, ProductIncludes includes) =>
        Box(new GetProductQueryHandler(db).HandleAsync(new GetProductQuery(id, includes)));

    public static Task<object> ListProducts(ProductCatalogDbContext db, ListProductsQuery query) =>
        Box(new ListProductsQueryHandler(db).HandleAsync(query));

    public static Task<object> GetVariant(ProductCatalogDbContext db, Guid id) =>
        Box(new GetVariantQueryHandler(db).HandleAsync(new GetVariantQuery(id)));

    public static ICategoryService Categories(ProductCatalogDbContext db) => new CategoryService(db);

    public static IAttributeService Attributes(ProductCatalogDbContext db) => new AttributeService(db);

    private static async Task<object> Box<T>(Task<T> task) => (await task)!;
}
