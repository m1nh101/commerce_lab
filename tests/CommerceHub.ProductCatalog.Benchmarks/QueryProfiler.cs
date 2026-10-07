using System.Data.Common;
using System.Text;
using CommerceHub.ProductCatalog.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace CommerceHub.ProductCatalog.Benchmarks;

/// <summary>
/// Runs each query in <see cref="BenchQueries.All"/> once (after a warm-up) and reports the SQL it sent, the number of
/// round trips, their server time, and <c>EXPLAIN (ANALYZE, BUFFERS)</c> for every statement.
/// Surfaces N+1 patterns, sequential scans and missing indexes. Output: <c>BenchmarkDotNet.Artifacts/sql-profile.md</c>.
/// </summary>
internal static class QueryProfiler
{
    public static async Task RunAsync()
    {
        var data = await BenchDatabase.EnsureSeededAsync();
        var recorder = new CommandRecorder();
        var options = BenchDatabase.CreateOptions(b => b.AddInterceptors(recorder));
        var report = new StringBuilder("# Product Catalog SQL profile\n\n")
            .AppendLine($"Dataset: {BenchDatabase.ProductCount:N0} products x {BenchDatabase.VariantsPerProduct} variants.\n")
            .AppendLine("| Query | Round trips | DB time (ms) |")
            .AppendLine("|---|---:|---:|");
        var details = new StringBuilder();

        foreach (var (name, run) in BenchQueries.All)
        {
            await using (var warmUp = new ProductCatalogDbContext(options))
            {
                await run(warmUp, data);
            }

            recorder.Commands.Clear();
            await using (var db = new ProductCatalogDbContext(options))
            {
                await run(db, data);
            }

            var commands = recorder.Commands.ToList();
            var totalMs = commands.Sum(c => c.Duration.TotalMilliseconds);
            report.AppendLine($"| {name} | {commands.Count} | {totalMs:F2} |");
            Console.WriteLine($"{name,-30} round trips: {commands.Count,2}  db time: {totalMs,8:F2} ms");

            details.AppendLine($"\n## {name}\n");
            foreach (var command in commands)
            {
                details.AppendLine($"```sql\n{command.Sql}\n```\n")
                    .AppendLine($"Parameters: {string.Join(", ", command.Parameters.Select(p => $"{p.ParameterName}={p.Value}"))}\n")
                    .AppendLine($"```text\n{await ExplainAsync(command)}\n```");
            }
        }

        var path = Path.Combine("BenchmarkDotNet.Artifacts", "sql-profile.md");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, report.Append(details).ToString());
        Console.WriteLine($"\nSQL profile written to {Path.GetFullPath(path)}");
    }

    private static async Task<string> ExplainAsync(RecordedCommand command)
    {
        await using var connection = new NpgsqlConnection(BenchDatabase.ConnectionString);
        await connection.OpenAsync();
        await using var explain = new NpgsqlCommand($"EXPLAIN (ANALYZE, BUFFERS) {command.Sql}", connection);
        explain.Parameters.AddRange(command.Parameters.Select(p => p.Clone()).ToArray());

        var plan = new StringBuilder();
        await using var reader = await explain.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            plan.AppendLine(reader.GetString(0));
        }

        return plan.ToString().TrimEnd();
    }

    private sealed record RecordedCommand(string Sql, IReadOnlyList<NpgsqlParameter> Parameters, TimeSpan Duration);

    private sealed class CommandRecorder : DbCommandInterceptor
    {
        public List<RecordedCommand> Commands { get; } = [];

        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            Record(command, eventData);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<object?> ScalarExecutedAsync(
            DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
        {
            Record(command, eventData);
            return ValueTask.FromResult(result);
        }

        private void Record(DbCommand command, CommandExecutedEventData eventData) =>
            Commands.Add(new RecordedCommand(
                command.CommandText,
                command.Parameters.Cast<NpgsqlParameter>().Select(p => p.Clone()).ToList(),
                eventData.Duration));
    }
}
