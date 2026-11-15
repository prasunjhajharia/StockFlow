using Dapper;
using Npgsql;
using Xunit;

namespace StockFlow.Tests;

public class StockSqlTests
{
    private const string Receive =
        "SELECT record_stock_movement(@p, @w, 'RECEIPT'::movement_type, @qty, 'TEST', NULL)";
    private const string Sell =
        "SELECT record_stock_movement(@p, @w, 'SALE'::movement_type, @qty, 'TEST', NULL)";

    // Creates one warehouse, one product, and optionally some starting stock
    private static async Task<(int P, int W)> SeedAsync(int startingStock = 0)
    {
        await using var conn = await TestDb.DataSource.OpenConnectionAsync();
        var w = await conn.ExecuteScalarAsync<int>(
            "INSERT INTO warehouses (name) VALUES ('W1') RETURNING id");
        var p = await conn.ExecuteScalarAsync<int>(
            "INSERT INTO products (sku, name, unit_price) VALUES ('SKU-1', 'Item', 10) RETURNING id");

        if (startingStock > 0)
            await conn.ExecuteScalarAsync<long>(Receive, new { p, w, qty = startingStock });

        return (p, w);
    }

    private static async Task<int> QuantityAsync(int p, int w)
    {
        await using var conn = await TestDb.DataSource.OpenConnectionAsync();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COALESCE(SUM(quantity), 0) FROM stock_levels WHERE product_id = @p AND warehouse_id = @w",
            new { p, w });
    }

    [Fact]
    public async Task Receipt_increases_stock_and_writes_one_ledger_row()
    {
        await TestDb.ResetAsync();
        var (p, w) = await SeedAsync(startingStock: 10);

        Assert.Equal(10, await QuantityAsync(p, w));

        await using var conn = await TestDb.DataSource.OpenConnectionAsync();
        var rows = await conn.ExecuteScalarAsync<int>(
            "SELECT count(*) FROM stock_movements WHERE product_id = @p", new { p });
        Assert.Equal(1, rows);
    }

    [Fact]
    public async Task Selling_more_than_available_is_refused_and_stock_is_unchanged()
    {
        await TestDb.ResetAsync();
        var (p, w) = await SeedAsync(startingStock: 5);

        await using var conn = await TestDb.DataSource.OpenConnectionAsync();
        var ex = await Assert.ThrowsAsync<PostgresException>(
            () => conn.ExecuteScalarAsync<long>(Sell, new { p, w, qty = -10 }));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
        Assert.Equal(5, await QuantityAsync(p, w));
    }

    [Fact]
    public async Task Ledger_rows_cannot_be_updated_or_deleted()
    {
        await TestDb.ResetAsync();
        await SeedAsync(startingStock: 5);

        await using var conn = await TestDb.DataSource.OpenConnectionAsync();
        await Assert.ThrowsAsync<PostgresException>(
            () => conn.ExecuteAsync("UPDATE stock_movements SET quantity_change = 99"));
        await Assert.ThrowsAsync<PostgresException>(
            () => conn.ExecuteAsync("DELETE FROM stock_movements"));
    }

    // The interview test: 25 buyers race for 10 items. Exactly 10 must win.
    [Fact]
    public async Task Concurrent_sales_never_oversell()
    {
        await TestDb.ResetAsync();
        var (p, w) = await SeedAsync(startingStock: 10);

        var attempts = Enumerable.Range(0, 25).Select(async _ =>
        {
            await using var conn = await TestDb.DataSource.OpenConnectionAsync();
            try
            {
                await conn.ExecuteScalarAsync<long>(Sell, new { p, w, qty = -1 });
                return true;
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.CheckViolation)
            {
                return false;
            }
        });

        var results = await Task.WhenAll(attempts);

        Assert.Equal(10, results.Count(ok => ok));
        Assert.Equal(15, results.Count(ok => !ok));
        Assert.Equal(0, await QuantityAsync(p, w));
    }

    [Fact]
    public async Task Receiving_a_purchase_order_twice_only_adds_stock_once()
    {
        await TestDb.ResetAsync();
        var (p, w) = await SeedAsync();

        await using var conn = await TestDb.DataSource.OpenConnectionAsync();
        await conn.ExecuteAsync("INSERT INTO suppliers (name) VALUES ('Acme')");
        var po = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO purchase_orders (supplier_id, warehouse_id, status, ordered_at)
            VALUES (1, @w, 'ORDERED', now()) RETURNING id", new { w });
        await conn.ExecuteAsync(@"
            INSERT INTO purchase_order_lines (purchase_order_id, product_id, quantity, unit_cost)
            VALUES (@po, @p, 12, 5)", new { po, p });

        var received = await conn.ExecuteScalarAsync<int>("SELECT receive_purchase_order(@po)", new { po });
        Assert.Equal(1, received);

        await Assert.ThrowsAsync<PostgresException>(
            () => conn.ExecuteScalarAsync<int>("SELECT receive_purchase_order(@po)", new { po }));

        Assert.Equal(12, await QuantityAsync(p, w));
    }

    [Fact]
    public async Task Shipping_an_order_with_too_little_stock_changes_nothing()
    {
        await TestDb.ResetAsync();
        var (p, w) = await SeedAsync(startingStock: 10);

        await using var conn = await TestDb.DataSource.OpenConnectionAsync();
        await conn.ExecuteAsync("INSERT INTO customers (name) VALUES ('Jens')");
        var so = await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO sales_orders (customer_id, warehouse_id, status)
            VALUES (1, @w, 'CONFIRMED') RETURNING id", new { w });
        await conn.ExecuteAsync(@"
            INSERT INTO sales_order_lines (sales_order_id, product_id, quantity, unit_price)
            VALUES (@so, @p, 30, 10)", new { so, p });

        await Assert.ThrowsAsync<PostgresException>(
            () => conn.ExecuteScalarAsync<int>("SELECT ship_sales_order(@so)", new { so }));

        var status = await conn.ExecuteScalarAsync<string>(
            "SELECT status::text FROM sales_orders WHERE id = @so", new { so });
        Assert.Equal("CONFIRMED", status);
        Assert.Equal(10, await QuantityAsync(p, w));
    }
}