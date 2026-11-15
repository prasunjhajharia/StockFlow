using Dapper;
using Npgsql;
using StockFlow.Core.Exceptions;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Data.Repositories;

public class ProductRepository(IDbConnectionFactory db) : IProductRepository
{
    private const string Columns =
        "id, sku, name, description, unit_price, reorder_level, is_active, created_at, updated_at";

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<Product>(new CommandDefinition(
            $"SELECT {Columns} FROM products ORDER BY id", cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<Product>(new CommandDefinition(
            $"SELECT {Columns} FROM products WHERE id = @id", new { id }, cancellationToken: ct));
    }

    public async Task<Product> CreateAsync(Product p, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        try
        {
            return await conn.QuerySingleAsync<Product>(new CommandDefinition(
                $@"INSERT INTO products (sku, name, description, unit_price, reorder_level)
                   VALUES (@Sku, @Name, @Description, @UnitPrice, @ReorderLevel)
                   RETURNING {Columns}", p, cancellationToken: ct));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateSkuException(p.Sku);
        }
    }

    public async Task<Product?> UpdateAsync(Product p, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        try
        {
            return await conn.QuerySingleOrDefaultAsync<Product>(new CommandDefinition(
                $@"UPDATE products
                   SET sku = @Sku, name = @Name, description = @Description,
                       unit_price = @UnitPrice, reorder_level = @ReorderLevel, is_active = @IsActive
                   WHERE id = @Id
                   RETURNING {Columns}", p, cancellationToken: ct));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DuplicateSkuException(p.Sku);
        }
    }
}