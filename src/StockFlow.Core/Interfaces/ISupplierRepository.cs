using StockFlow.Core.Models;

namespace StockFlow.Core.Interfaces;

public interface ISupplierRepository
{
    Task<IReadOnlyList<Supplier>> GetAllAsync(CancellationToken ct = default);
    Task<Supplier?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Supplier> CreateAsync(Supplier supplier, CancellationToken ct = default);
    Task<Supplier?> UpdateAsync(Supplier supplier, CancellationToken ct = default);
}