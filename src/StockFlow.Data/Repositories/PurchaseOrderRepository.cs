using Dapper;
using Npgsql;
using StockFlow.Core.Exceptions;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Data.Repositories;

public class PurchaseOrderRepository(IDbConnectionFactory db) : IPurchaseOrderRepository
{
    private const string HeaderColumns =
        "id, supplier_id, warehouse_id, status::text AS status, ordered_at, received_at, created_at, updated_at";

    public async Task<IReadOnlyList<PurchaseOrder>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<PurchaseOrder>(new CommandDefinition(
            $"SELECT {HeaderColumns} FROM purchase_orders ORDER BY id DESC", cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<PurchaseOrder?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var po = await conn.QuerySingleOrDefaultAsync<PurchaseOrder>(new CommandDefinition(
            $"SELECT {HeaderColumns} FROM purchase_orders WHERE id = @id", new { id }, cancellationToken: ct));
        if (po is null) return null;

        var lines = await conn.QueryAsync<PurchaseOrderLine>(new CommandDefinition(
            @"SELECT id, purchase_order_id, product_id, quantity, unit_cost
              FROM purchase_order_lines WHERE purchase_order_id = @id ORDER BY id",
            new { id }, cancellationToken: ct));
        po.Lines = lines.ToList();
        return po;
    }

    public async Task<PurchaseOrder> CreateAsync(int supplierId, int warehouseId,
        IReadOnlyList<PurchaseOrderLine> lines, CancellationToken ct = default)
    {
        int newId;
        await using (var conn = await db.OpenConnectionAsync(ct))
        await using (var tx = await conn.BeginTransactionAsync(ct))
        {
            try
            {
                newId = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                    @"INSERT INTO purchase_orders (supplier_id, warehouse_id)
                      VALUES (@supplierId, @warehouseId) RETURNING id",
                    new { supplierId, warehouseId }, transaction: tx, cancellationToken: ct));

                foreach (var l in lines)
                {
                    await conn.ExecuteAsync(new CommandDefinition(
                        @"INSERT INTO purchase_order_lines (purchase_order_id, product_id, quantity, unit_cost)
                          VALUES (@newId, @ProductId, @Quantity, @UnitCost)",
                        new { newId, l.ProductId, l.Quantity, l.UnitCost },
                        transaction: tx, cancellationToken: ct));
                }

                await tx.CommitAsync(ct);   // without this, disposing the transaction rolls everything back
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            {
                throw new NotFoundException("Supplier, warehouse or product not found.");
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw new ConflictException("The same product appears twice in the order lines.");
            }
        }

        return (await GetByIdAsync(newId, ct))!;
    }

    public async Task MarkOrderedAsync(int id, CancellationToken ct = default)
        => await ChangeStatusAsync(id, "ORDERED", "DRAFT", ", ordered_at = now()", ct);

    public async Task CancelAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var changed = await conn.ExecuteAsync(new CommandDefinition(
            @"UPDATE purchase_orders SET status = 'CANCELLED'
              WHERE id = @id AND status IN ('DRAFT', 'ORDERED')",
            new { id }, cancellationToken: ct));
        if (changed == 0) await ThrowNotFoundOrConflict(conn, id, "cancel", ct);
    }

    public async Task<int> ReceiveAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        try
        {
            // Your PL/pgSQL function: lock the order, add stock for each line, mark RECEIVED
            return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT receive_purchase_order(@id)", new { id }, cancellationToken: ct));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.RaiseException)
        {
            // Plain RAISE EXCEPTION in the function uses code P0001
            if (ex.MessageText.EndsWith("not found"))
                throw new NotFoundException(ex.MessageText);
            throw new ConflictException(ex.MessageText);
        }
    }

    private async Task ChangeStatusAsync(int id, string newStatus, string requiredStatus,
        string extraSet, CancellationToken ct)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var changed = await conn.ExecuteAsync(new CommandDefinition(
            $@"UPDATE purchase_orders SET status = '{newStatus}'{extraSet}
               WHERE id = @id AND status = '{requiredStatus}'",
            new { id }, cancellationToken: ct));
        if (changed == 0) await ThrowNotFoundOrConflict(conn, id, "order", ct);
    }

    private static async Task ThrowNotFoundOrConflict(
        System.Data.Common.DbConnection conn, int id, string action, CancellationToken ct)
    {
        var status = await conn.ExecuteScalarAsync<string?>(new CommandDefinition(
            "SELECT status::text FROM purchase_orders WHERE id = @id", new { id }, cancellationToken: ct));
        if (status is null) throw new NotFoundException($"Purchase order {id} not found.");
        throw new ConflictException($"Purchase order {id} is {status} and cannot be {action}ed.");
    }
}