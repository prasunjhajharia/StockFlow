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

    public async Task<AppUser?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<AppUser>(new CommandDefinition(@"
            SELECT u.id, u.email, u.password_hash, u.full_name,
                   r.name AS role_name, u.is_active, u.locked_until
            FROM users u
            JOIN roles r ON r.id = u.role_id
            WHERE u.id = @id",
            new { id }, cancellationToken: ct));
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
        await conn.ExecuteAsync(new CommandDefinition(
            "SELECT record_login_attempt(@email, @succeeded, @ip, @userAgent)",
            new { email, succeeded, ip, userAgent }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<UserSummary>> GetAllAsync(CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<UserSummary>(new CommandDefinition(@"
            SELECT u.id, u.email, u.full_name, r.name AS role_name,
                   u.is_active, u.last_login_at, u.created_at
            FROM users u
            JOIN roles r ON r.id = u.role_id
            ORDER BY u.id", cancellationToken: ct));
        return rows.ToList();
    }

    public async Task ChangeRoleAsync(int actorId, int userId, string roleName, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        try
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "SELECT change_user_role(@actorId, @userId, @roleName)",
                new { actorId, userId, roleName }, cancellationToken: ct));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.RaiseException)
        {
            throw MapRuleError(ex);
        }
    }

    public async Task SetActiveAsync(int actorId, int userId, bool active, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        try
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "SELECT set_user_active(@actorId, @userId, @active)",
                new { actorId, userId, active }, cancellationToken: ct));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.RaiseException)
        {
            throw MapRuleError(ex);
        }
    }

    private static Exception MapRuleError(PostgresException ex)
        => ex.MessageText.EndsWith("not found")
            ? new NotFoundException(ex.MessageText)
            : new ConflictException(ex.MessageText);
}