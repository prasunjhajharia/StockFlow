using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using StockFlow.Data;
using Xunit;

// The tests share one database, so they must not run at the same time
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace StockFlow.Tests;

public static class TestDb
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static bool _schemaReady;

    public static NpgsqlDataSource DataSource { get; private set; } = null!;
    public static IDbConnectionFactory Factory { get; private set; } = null!;

    // Call at the start of every test: builds the schema once, then empties all data
    public static async Task ResetAsync()
    {
        await Gate.WaitAsync();
        try
        {
            if (!_schemaReady)
            {
                await BuildSchemaAsync();
                _schemaReady = true;
            }

            await using var conn = await DataSource.OpenConnectionAsync();
            // 'roles' is reference data created by 007, so it is kept
            await conn.ExecuteAsync(@"
                TRUNCATE sales_order_lines, sales_orders, purchase_order_lines, purchase_orders,
                         stock_movements, stock_levels, products, warehouses, suppliers, customers,
                         refresh_tokens, login_history, users
                RESTART IDENTITY CASCADE");
        }
        finally
        {
            Gate.Release();
        }
    }

    private static async Task BuildSchemaAsync()
    {
        var config = new ConfigurationBuilder().AddUserSecrets(typeof(TestDb).Assembly).Build();
        var cs = Environment.GetEnvironmentVariable("STOCKFLOW_TEST_CONNECTION")
            ?? config.GetConnectionString("StockFlowTest")
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings:StockFlowTest with dotnet user-secrets (see the setup step).");


        var database = new NpgsqlConnectionStringBuilder(cs).Database ?? "";
        if (!database.EndsWith("_test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Refusing to wipe '{database}'. The test database name must end with '_test'.");

        DataSource = NpgsqlDataSource.Create(cs);
        Factory = new NpgsqlConnectionFactory(DataSource);

        await using var conn = await DataSource.OpenConnectionAsync();
        await conn.ExecuteAsync("DROP SCHEMA public CASCADE; CREATE SCHEMA public;");

        // Run 001..009 in order (seed_dev.sql does not match the pattern, so it is skipped)
        var dbFolder = Path.Combine(FindRepoRoot(), "database");
        var files = Directory.GetFiles(dbFolder, "*.sql")
            .Where(f => System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(f), @"^\d{3}_"))
            .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
            .ToList();

        if (files.Count == 0)
            throw new InvalidOperationException($"No numbered .sql files found in {dbFolder}");

        foreach (var file in files)
            await conn.ExecuteAsync(await File.ReadAllTextAsync(file));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "StockFlow.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not find StockFlow.slnx.");
    }
}