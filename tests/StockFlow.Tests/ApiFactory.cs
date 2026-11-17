using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace StockFlow.Tests;

// Starts the real API in memory, pointed at the TEST database
public class ApiFactory : WebApplicationFactory<Program>
{
    public ApiFactory()
    {
        var cs = Environment.GetEnvironmentVariable("STOCKFLOW_TEST_CONNECTION")
            ?? new ConfigurationBuilder().AddUserSecrets(typeof(TestDb).Assembly).Build()
                .GetConnectionString("StockFlowTest")
            ?? throw new InvalidOperationException("Test connection string is missing.");

        // Safety: never point the API at a database that is not a test database
        var database = new NpgsqlConnectionStringBuilder(cs).Database ?? "";
        if (!database.EndsWith("_test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Refusing to use '{database}' for API tests.");

        // Set BEFORE the API starts, so they beat user secrets
        Environment.SetEnvironmentVariable("ConnectionStrings__StockFlow", cs);
        Environment.SetEnvironmentVariable("Jwt__Key", "test-only-signing-key-that-is-at-least-40-characters-long");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "StockFlow");
        Environment.SetEnvironmentVariable("Jwt__Audience", "StockFlow.Client");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.UseEnvironment("Development");
}