using BenchmarkDotNet.Attributes;
using CommerceHub.ProductCatalog.Application.Products;
using CommerceHub.ProductCatalog.Application.Products.Queries;
using CommerceHub.ProductCatalog.Domain.Products;

namespace CommerceHub.ProductCatalog.Benchmarks.Products;

public class ProductQueryBenchmarks : QueryBenchmarkBase
{
    [Params(20, 100)]
    public int Limit { get; set; }

    [Benchmark(Baseline = true)]
    public Task<object> GetProduct_NoIncludes() =>
        RunAsync(db => BenchQueries.GetProduct(db, Data.ProductId, ProductIncludes.None));

    [Benchmark]
    public Task<object> GetProduct_AllIncludes() =>
        RunAsync(db => BenchQueries.GetProduct(db, Data.ProductId, ProductIncludes.Variants | ProductIncludes.Categories));

    [Benchmark]
    public Task<object> GetProduct_NotFound() =>
        RunAsync(db => BenchQueries.GetProduct(db, Guid.Empty, ProductIncludes.None));

    [Benchmark]
    public Task<object> ListProducts_FirstPage() =>
        RunAsync(db => BenchQueries.ListProducts(db, new ListProductsQuery(null, null, null, null, Limit: Limit)));

    [Benchmark]
    public Task<object> ListProducts_DeepPage() =>
        RunAsync(db => BenchQueries.ListProducts(db, new ListProductsQuery(
            null, null, null, null, Page: BenchDatabase.ProductCount / Limit / 2, Limit: Limit)));

    [Benchmark]
    public Task<object> ListProducts_ByStatus() =>
        RunAsync(db => BenchQueries.ListProducts(db, new ListProductsQuery(ProductStatus.Active, null, null, null, Limit: Limit)));

    [Benchmark]
    public Task<object> ListProducts_ByCategory() =>
        RunAsync(db => BenchQueries.ListProducts(db, new ListProductsQuery(null, Data.BusiestCategoryId, null, null, Limit: Limit)));

    [Benchmark]
    public Task<object> ListProducts_Search() =>
        RunAsync(db => BenchQueries.ListProducts(db, new ListProductsQuery(null, null, "duct 05", null, ProductSort.NameAsc, Limit: Limit)));

    [Benchmark]
    public Task<object> ListProducts_BySlug() =>
        RunAsync(db => BenchQueries.ListProducts(db, new ListProductsQuery(null, null, null, Data.ProductSlug, Limit: Limit)));
}
