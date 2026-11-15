namespace StockFlow.Core.Interfaces;

public interface IRefreshTokenRepository
{
    Task StoreAsync(int userId, string tokenHash, DateTime expiresAt, CancellationToken ct = default);

    // Returns the user id, or null if the token must be rejected
    Task<int?> RotateAsync(string oldHash, string newHash, DateTime newExpiresAt, CancellationToken ct = default);

    Task RevokeAsync(string tokenHash, CancellationToken ct = default);
}