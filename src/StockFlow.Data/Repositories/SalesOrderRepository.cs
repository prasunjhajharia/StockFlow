using Dapper;
using Npgsql;
using StockFlow.Core.Exceptions;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Data.Repositories;

public class SalesOrderRepository(IDbConnectionFactory db) : ISalesOrderRepository
{
    private const string HeaderColumns =
        "id, customer_id, warehouse_id, status::text AS status, shipped_at, created_at, updated_at";

    public async Task<IReadOnlyList<SalesOrder>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<SalesOrder>(new CommandDefinition(
            $"SELECT {HeaderColumns} FROM sales_orders ORDER BY id DESC", cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<SalesOrder?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var so = await conn.QuerySingleOrDefaultAsync<SalesOrder>(new CommandDefinition(
            $"SELECT {HeaderColumns} FROM sales_orders WHERE id = @id", new { id }, cancellationToken: ct));
        if (so is null) return null;

        var lines = await conn.QueryAsync<SalesOrderLine>(new CommandDefinition(
            @"SELECT id, sales_order_id, product_id, quantity, unit_price
              FROM sales_order_lines WHERE sales_order_id = @id ORDER BY id",
            new { id }, cancellationToken: ct));
        so.Lines = lines.ToList();
        return so;
    }

    public async Task<SalesOrder> CreateAsync(int customerId, int warehouseId,
        IReadOnlyList<SalesOrderLine> lines, CancellationToken ct = default)
    {
        int newId;
        await using (var conn = await db.OpenConnectionAsync(ct))
        await using (var tx = await conn.BeginTransactionAsync(ct))
        {
            try
            {
                newId = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                    @"INSERT INTO sales_orders (customer_id, warehouse_id)
                      VALUES (@customerId, @warehouseId) RETURNING id",
                    new { customerId, warehouseId }, transaction: tx, cancellationToken: ct));

                foreach (var l in lines)
                {
                    // The price is copied from the product at this moment
                    var inserted = await conn.ExecuteAsync(new CommandDefinition(
                        @"INSERT INTO sales_order_lines (sales_order_id, product_id, quantity, unit_price)
                          SELECT @newId, p.id, @Quantity, p.unit_price
                          FROM products p
                          WHERE p.id = @ProductId AND p.is_active",
                        new { newId, l.ProductId, l.Quantity }, transaction: tx, cancellationToken: ct));

                    if (inserted == 0)
                        throw new NotFoundException($"Product {l.ProductId} not found or inactive.");
                }

                await tx.CommitAsync(ct);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            {
                throw new NotFoundException("Customer or warehouse not found.");
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw new ConflictException("The same product appears twice in the order lines.");
            }
        }

        return (await GetByIdAsync(newId, ct))!;
    }

    public async Task ConfirmAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var changed = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE sales_orders SET status = 'CONFIRMED' WHERE id = @id AND status = 'DRAFT'",
            new { id }, cancellationToken: ct));
        if (changed == 0) await ThrowNotFoundOrConflict(conn, id, "confirmed", ct);
    }

    public async Task CancelAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var changed = await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE sales_orders SET status = 'CANCELLED'
              WHERE id = @id AND status IN ('DRAFT', 'CONFIRMED')",
            new { id }, cancellationToken: ct));
        if (changed == 0) await ThrowNotFoundOrConflict(conn, id, "cancelled", ct);
    }

    public async Task<int> ShipAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        try
        {
            // Your PL/pgSQL function: lock the order, remove stock per line, mark SHIPPED
            return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT ship_sales_order(@id)", new { id }, cancellationToken: ct));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.CheckViolation)
        {
            // "Insufficient stock" is raised with the check_violation code
            throw new ConflictException(ex.MessageText);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.RaiseException)
        {
            if (ex.MessageText.EndsWith("not found"))
                throw new NotFoundException(ex.MessageText);
            throw new ConflictException(ex.MessageText);
        }
    }

    private static async Task ThrowNotFoundOrConflict(
        System.Data.Common.DbConnection conn, int id, string action, CancellationToken ct)
    {
        var status = await conn.ExecuteScalarAsync<string?>(new CommandDefinition(
            "SELECT status::text FROM sales_orders WHERE id = @id", new { id }, cancellationToken: ct));
        if (status is null) throw new NotFoundException($"Sales order {id} not found.");
        throw new ConflictException($"Sales order {id} is {status} and cannot be {action}.");
    }
}