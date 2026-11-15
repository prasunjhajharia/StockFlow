using StockFlow.Core.Models;

namespace StockFlow.Core.Interfaces;

public interface IUserRepository
{
    Task<AppUser?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<AppUser?> FindByIdAsync(int id, CancellationToken ct = default);
    Task<AppUser> CreateAsync(string email, string passwordHash, string fullName,
        string roleName, CancellationToken ct = default);
    Task RecordLoginAttemptAsync(string email, bool succeeded, string? ip,
        string? userAgent, CancellationToken ct = default);

    Task<IReadOnlyList<UserSummary>> GetAllAsync(CancellationToken ct = default);
    Task ChangeRoleAsync(int actorId, int userId, string roleName, CancellationToken ct = default);
    Task SetActiveAsync(int actorId, int userId, bool active, CancellationToken ct = default);
}