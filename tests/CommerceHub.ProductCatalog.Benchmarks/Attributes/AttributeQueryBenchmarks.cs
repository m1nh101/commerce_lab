using BenchmarkDotNet.Attributes;
using CommerceHub.ProductCatalog.Application.Attributes;

namespace CommerceHub.ProductCatalog.Benchmarks.Attributes;

public class AttributeQueryBenchmarks : QueryBenchmarkBase
{
    [Benchmark(Baseline = true)]
    public Task<object> GetAttribute() =>
        RunAsync(async db => await BenchQueries.Attributes(db).GetAsync(Data.AttributeId));

    [Benchmark]
    public Task<object> ListAttributes() =>
        RunAsync(async db => await BenchQueries.Attributes(db).ListAsync(new AttributeListQuery(null, 1, 20)));

    [Benchmark]
    public Task<object> ListAttributes_Search() =>
        RunAsync(async db => await BenchQueries.Attributes(db).ListAsync(new AttributeListQuery("attr_1", 1, 20)));
}
