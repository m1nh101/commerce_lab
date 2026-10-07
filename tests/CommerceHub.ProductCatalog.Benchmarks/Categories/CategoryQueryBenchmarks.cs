using BenchmarkDotNet.Attributes;
using CommerceHub.ProductCatalog.Application.Categories;

namespace CommerceHub.ProductCatalog.Benchmarks.Categories;

public class CategoryQueryBenchmarks : QueryBenchmarkBase
{
    private static readonly CategoryListQuery AllCategories = new(false, null, null, null);

    [Benchmark(Baseline = true)]
    public Task<object> GetCategory_ById() =>
        RunAsync(async db => await BenchQueries.Categories(db).GetAsync(Data.CategoryId.ToString()));

    [Benchmark]
    public Task<object> GetCategory_BySlug() =>
        RunAsync(async db => await BenchQueries.Categories(db).GetAsync(Data.CategorySlug));

    [Benchmark]
    public Task<object> ListCategories() =>
        RunAsync(async db => await BenchQueries.Categories(db).ListAsync(AllCategories));

    [Benchmark]
    public Task<object> ListCategories_TopLevel() =>
        RunAsync(async db => await BenchQueries.Categories(db).ListAsync(new CategoryListQuery(true, null, null, null)));

    [Benchmark]
    public Task<object> GetCategoryTree() =>
        RunAsync(async db => await BenchQueries.Categories(db).GetTreeAsync(AllCategories));
}
