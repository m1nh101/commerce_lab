using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using CommerceHub.ProductCatalog.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CommerceHub.ProductCatalog.Benchmarks;

/// <summary>
/// Seeds the bench database once and hands each invocation a fresh context from a pool (as a request scope would).
/// Allocations are reported per call; a CPU-sampling .nettrace is written per benchmark for profiling.
/// </summary>
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
public abstract class QueryBenchmarkBase
{
    private IDbContextFactory<ProductCatalogDbContext> _factory = null!;

    private protected BenchData Data { get; private set; } = null!;

    [GlobalSetup]
    public async Task GlobalSetupAsync()
    {
        Data = await BenchDatabase.EnsureSeededAsync();
        _factory = new PooledDbContextFactory<ProductCatalogDbContext>(BenchDatabase.CreateOptions());
    }

    private protected async Task<object> RunAsync(Func<ProductCatalogDbContext, Task<object>> query)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await query(db);
    }
}
