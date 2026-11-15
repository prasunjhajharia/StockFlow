using Dapper;
using Npgsql;
using StockFlow.Core.Exceptions;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Data.Repositories;

public class SupplierRepository(IDbConnectionFactory db) : ISupplierRepository
{
    private const string Columns = "id, name, email, phone, is_active, created_at";

    public async Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Supplier>(new CommandDefinition(
            $"SELECT {Columns} FROM suppliers ORDER BY id", cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<Supplier?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Supplier>(new CommandDefinition(
            $"SELECT {Columns} FROM suppliers WHERE id = @id", new { id }, cancellationToken: ct));
    }

    public async Task<Supplier> CreateAsync(Supplier s, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        try
        {
            return await conn.QuerySingleAsync<Supplier>(new CommandDefinition(
                $@"INSERT INTO suppliers (name, email, phone)
                   VALUES (@Name, @Email, @Phone)
                   RETURNING {Columns}", s, cancellationToken: ct));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflictException($"A supplier named '{s.Name}' already exists.");
        }
    }

    public async Task<Supplier?> UpdateAsync(Supplier s, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        try
        {
            return await conn.QuerySingleOrDefaultAsync<Supplier>(new CommandDefinition(
                $@"UPDATE suppliers
                   SET name = @Name, email = @Email, phone = @Phone, is_active = @IsActive
                   WHERE id = @Id
                   RETURNING {Columns}", s, cancellationToken: ct));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflictException($"A supplier named '{s.Name}' already exists.");
        }
    }
}