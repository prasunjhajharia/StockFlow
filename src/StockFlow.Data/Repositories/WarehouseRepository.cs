using Dapper;
using Npgsql;
using StockFlow.Core.Exceptions;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Data.Repositories;

public class WarehouseRepository(IDbConnectionFactory db) : IWarehouseRepository
{
    private const string Columns = "id, name, location, is_active, created_at";

    public async Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Warehouse>(new CommandDefinition(
            $"SELECT {Columns} FROM warehouses ORDER BY id", cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<Warehouse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Warehouse>(new CommandDefinition(
            $"SELECT {Columns} FROM warehouses WHERE id = @id", new { id }, cancellationToken: ct));
    }

    public async Task<Warehouse> CreateAsync(Warehouse w, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        try
        {
            return await conn.QuerySingleAsync<Warehouse>(new CommandDefinition(
                $@"INSERT INTO warehouses (name, location)
                   VALUES (@Name, @Location)
                   RETURNING {Columns}", w, cancellationToken: ct));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateWarehouseNameException(w.Name);
        }
    }

    public async Task<Warehouse?> UpdateAsync(Warehouse w, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        try
        {
            return await conn.QuerySingleOrDefaultAsync<Warehouse>(new CommandDefinition(
                $@"UPDATE warehouses
                   SET name = @Name, location = @Location, is_active = @IsActive
                   WHERE id = @Id
                   RETURNING {Columns}", w, cancellationToken: ct));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateWarehouseNameException(w.Name);
        }
    }
}