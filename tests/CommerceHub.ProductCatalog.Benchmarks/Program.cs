using BenchmarkDotNet.Running;
using CommerceHub.ProductCatalog.Benchmarks;

// `--profile` prints SQL, round trips and EXPLAIN plans per query; anything else goes to BenchmarkDotNet (e.g. --filter *Product*).
if (args.Contains("--profile"))
{
    await QueryProfiler.RunAsync();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
