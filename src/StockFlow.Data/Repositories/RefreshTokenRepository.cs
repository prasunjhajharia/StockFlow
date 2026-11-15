using Dapper;
using StockFlow.Core.Interfaces;

namespace StockFlow.Data.Repositories;

public class RefreshTokenRepository(IDbConnectionFactory db) : IRefreshTokenRepository
{
    public async Task StoreAsync(int userId, string tokenHash, DateTime expiresAt, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "INSERT INTO refresh_tokens (user_id, token_hash, expires_at) VALUES (@userId, @tokenHash, @expiresAt)",
            new { userId, tokenHash, expiresAt }, cancellationToken: ct));
    }

    public async Task<int?> RotateAsync(string oldHash, string newHash, DateTime newExpiresAt, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        // Your PL/pgSQL function does the lock, checks, revoke and insert in one transaction
        return await conn.ExecuteScalarAsync<int?>(new CommandDefinition(
            "SELECT rotate_refresh_token(@oldHash, @newHash, @newExpiresAt)",
            new { oldHash, newHash, newExpiresAt }, cancellationToken: ct));
    }

    public async Task RevokeAsync(string tokenHash, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE refresh_tokens SET revoked_at = now() WHERE token_hash = @tokenHash AND revoked_at IS NULL",
            new { tokenHash }, cancellationToken: ct));
    }
}