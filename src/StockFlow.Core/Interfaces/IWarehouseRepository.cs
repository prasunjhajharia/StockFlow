using StockFlow.Core.Models;

namespace StockFlow.Core.Interfaces;

public interface IWarehouseRepository
{
    Task<IReadOnlyList<Warehouse>> GetAllAsync(CancellationToken ct = default);
    Task<Warehouse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Warehouse> CreateAsync(Warehouse warehouse, CancellationToken ct = default);
    Task<Warehouse?> UpdateAsync(Warehouse warehouse, CancellationToken ct = default);
}