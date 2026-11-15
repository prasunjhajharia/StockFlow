using Dapper;
using Npgsql;
using StockFlow.Core.Exceptions;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Data.Repositories;

public class UserRepository(IDbConnectionFactory db) : IUserRepository
{
    public async Task<AppUser?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<AppUser>(new CommandDefinition(@"
            SELECT u.id, u.email, u.password_hash, u.full_name,
                   r.name AS role_name, u.is_active, u.locked_until
            FROM users u
            JOIN roles r ON r.id = u.role_id
            WHERE u.email = @email",
            new { email }, cancellationToken: ct));
    }

    public async Task<AppUser> CreateAsync(string email, string passwordHash, string fullName,
        string roleName, CancellationToken ct = default)
    {
        await using (var conn = await db.OpenConnectionAsync(ct))
        {
            try
            {
                var id = await conn.ExecuteScalarAsync<int?>(new CommandDefinition(@"
                    INSERT INTO users (email, password_hash, full_name, role_id)
                    SELECT @email, @passwordHash, @fullName, r.id
                    FROM roles r WHERE r.name = @roleName
                    RETURNING id",
                    new { email, passwordHash, fullName, roleName }, cancellationToken: ct));

                if (id is null)
                    throw new NotFoundException($"Role '{roleName}' not found.");
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw new ConflictException("This email is already registered.");
            }
        }

        return (await FindByEmailAsync(email, ct))!;
    }

    public async Task RecordLoginAttemptAsync(string email, bool succeeded, string? ip,
        string? userAgent, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        // Your PL/pgSQL function: log the attempt and update the lockout counters
        await conn.ExecuteAsync(new CommandDefinition(
            "SELECT record_login_attempt(@email, @succeeded, @ip, @userAgent)",
            new { email, succeeded, ip, userAgent }, cancellationToken: ct));
    }
}