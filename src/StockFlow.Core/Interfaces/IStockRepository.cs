using StockFlow.Core.Models;

namespace StockFlow.Core.Interfaces;

public interface IStockRepository
{
    Task<IReadOnlyList<StockLevel>> GetLevelsAsync(int? warehouseId, CancellationToken ct = default);
    Task<IReadOnlyList<StockMovement>> GetMovementsAsync(int productId, CancellationToken ct = default);
    Task<long> AdjustAsync(int productId, int warehouseId, int quantityChange, string? note, CancellationToken ct = default);
}