using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Xunit;

namespace StockFlow.Tests;

public class ApiTests : IClassFixture<ApiFactory>
{
    private const string Password = "test-password-123";
    private readonly ApiFactory _factory;

    public ApiTests(ApiFactory factory) => _factory = factory;

    // Inserts a user straight into the database, with a real BCrypt hash
    private static async Task CreateUserAsync(string email, string role)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(Password);
        await using var conn = await TestDb.DataSource.OpenConnectionAsync();
        await conn.ExecuteAsync(@"
            INSERT INTO users (email, password_hash, full_name, role_id)
            SELECT @email, @hash, 'Test User', r.id FROM roles r WHERE r.name = @role",
            new { email, hash, role });
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password = Password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("accessToken").GetString()!;
    }

    private static HttpClient WithToken(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Products_without_a_token_returns_401()
    {
        await TestDb.ResetAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        await TestDb.ResetAsync();
        await CreateUserAsync("staff@test.local", "Staff");
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "staff@test.local", password = "definitely-wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Staff_can_read_products_but_not_create_them()
    {
        await TestDb.ResetAsync();
        await CreateUserAsync("staff@test.local", "Staff");
        var client = _factory.CreateClient();
        WithToken(client, await LoginAsync(client, "staff@test.local"));

        var read = await client.GetAsync("/api/products");
        var create = await client.PostAsJsonAsync("/api/products",
            new { sku = "X-1", name = "Nope", unitPrice = 1, reorderLevel = 0 });

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task Staff_cannot_view_reports()
    {
        await TestDb.ResetAsync();
        await CreateUserAsync("staff@test.local", "Staff");
        var client = _factory.CreateClient();
        WithToken(client, await LoginAsync(client, "staff@test.local"));

        var response = await client.GetAsync("/api/reports/low-stock");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Manager_can_create_a_product_and_a_duplicate_sku_returns_409()
    {
        await TestDb.ResetAsync();
        await CreateUserAsync("manager@test.local", "Manager");
        var client = _factory.CreateClient();
        WithToken(client, await LoginAsync(client, "manager@test.local"));

        var body = new { sku = "LAP-001", name = "Laptop", unitPrice = 899, reorderLevel = 5 };
        var first = await client.PostAsJsonAsync("/api/products", body);
        var second = await client.PostAsJsonAsync("/api/products", body);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Refresh_token_cannot_be_used_twice()
    {
        await TestDb.ResetAsync();
        await CreateUserAsync("staff@test.local", "Staff");
        var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "staff@test.local", password = Password });
        var refreshToken = (await login.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("refreshToken").GetString();

        var first = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
        var reuse = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_the_account()
    {
        await TestDb.ResetAsync();
        await CreateUserAsync("staff@test.local", "Staff");
        var client = _factory.CreateClient();

        for (var i = 0; i < 5; i++)
            await client.PostAsJsonAsync("/api/auth/login",
                new { email = "staff@test.local", password = "wrong-password-here" });

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "staff@test.local", password = Password });

        Assert.Equal((HttpStatusCode)423, response.StatusCode);
    }
}