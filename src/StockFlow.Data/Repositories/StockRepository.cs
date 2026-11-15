using Dapper;
using Npgsql;
using StockFlow.Core.Exceptions;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Data.Repositories;

public class StockRepository(IDbConnectionFactory db) : IStockRepository
{
    public async Task<IReadOnlyList<StockLevel>> GetLevelsAsync(int? warehouseId, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<StockLevel>(new CommandDefinition(@"
            SELECT s.product_id, p.sku, p.name AS product_name,
                   s.warehouse_id, w.name AS warehouse_name, s.quantity
            FROM stock_levels s
            JOIN products p   ON p.id = s.product_id
            JOIN warehouses w ON w.id = s.warehouse_id
            WHERE (@warehouseId IS NULL OR s.warehouse_id = @warehouseId)
            ORDER BY p.sku, w.name",
            new { warehouseId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<StockMovement>> GetMovementsAsync(int productId, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<StockMovement>(new CommandDefinition(@"
            SELECT id, product_id, warehouse_id, movement_type::text AS movement_type,
                   quantity_change, reference, note, created_at
            FROM stock_movements
            WHERE product_id = @productId
            ORDER BY id DESC",
            new { productId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<long> AdjustAsync(int productId, int warehouseId, int quantityChange, string? note, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        try
        {
            // Calls your PL/pgSQL function: lock, check, update, log
            return await conn.ExecuteScalarAsync<long>(new CommandDefinition(
                @"SELECT record_stock_movement(@productId, @warehouseId, 'ADJUSTMENT'::movement_type,
                                               @quantityChange, 'MANUAL', @note)",
                new { productId, warehouseId, quantityChange, note }, cancellationToken: ct));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.CheckViolation)
        {
            throw new ConflictException(ex.MessageText);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            throw new NotFoundException("Product or warehouse not found.");
        }
    }
}