using System.Data.Common;
using Npgsql;

namespace StockFlow.Data;

public interface IDbConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(CancellationToken ct = default);
}

public class NpgsqlConnectionFactory(NpgsqlDataSource dataSource) : IDbConnectionFactory
{
    public async Task<DbConnection> OpenConnectionAsync(CancellationToken ct = default)
        => await dataSource.OpenConnectionAsync(ct);
}