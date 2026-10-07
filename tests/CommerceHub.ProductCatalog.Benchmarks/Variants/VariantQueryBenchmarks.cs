using BenchmarkDotNet.Attributes;

namespace CommerceHub.ProductCatalog.Benchmarks.Variants;

public class VariantQueryBenchmarks : QueryBenchmarkBase
{
    [Benchmark(Baseline = true)]
    public Task<object> GetVariant() => RunAsync(db => BenchQueries.GetVariant(db, Data.VariantId));

    [Benchmark]
    public Task<object> GetVariant_NotFound() => RunAsync(db => BenchQueries.GetVariant(db, Guid.Empty));
}
