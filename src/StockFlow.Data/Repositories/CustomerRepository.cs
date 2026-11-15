using Dapper;
using Npgsql;
using StockFlow.Core.Exceptions;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Data.Repositories;

public class CustomerRepository(IDbConnectionFactory db) : ICustomerRepository
{
    private const string Columns = "id, name, email, phone, is_active, created_at";

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Customer>(new CommandDefinition(
            $"SELECT {Columns} FROM customers ORDER BY id", cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Customer>(new CommandDefinition(
            $"SELECT {Columns} FROM customers WHERE id = @id", new { id }, cancellationToken: ct));
    }

    public async Task<Customer> CreateAsync(Customer c, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        try
        {
            return await conn.QuerySingleAsync<Customer>(new CommandDefinition(
                $@"INSERT INTO customers (name, email, phone)
                   VALUES (@Name, @Email, @Phone)
                   RETURNING {Columns}", c, cancellationToken: ct));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflictException($"A customer with email '{c.Email}' already exists.");
        }
    }

    public async Task<Customer?> UpdateAsync(Customer c, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        try
        {
            return await conn.QuerySingleOrDefaultAsync<Customer>(new CommandDefinition(
                $@"UPDATE customers
                   SET name = @Name, email = @Email, phone = @Phone, is_active = @IsActive
                   WHERE id = @Id
                   RETURNING {Columns}", c, cancellationToken: ct));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new ConflictException($"A customer with email '{c.Email}' already exists.");
        }
    }
}