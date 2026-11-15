using StockFlow.Core.Models;

namespace StockFlow.Core.Interfaces;

public interface IPurchaseOrderRepository
{
    Task<IReadOnlyList<PurchaseOrder>> GetAllAsync(CancellationToken ct = default);
    Task<PurchaseOrder?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PurchaseOrder> CreateAsync(int supplierId, int warehouseId,
        IReadOnlyList<PurchaseOrderLine> lines, CancellationToken ct = default);
    Task MarkOrderedAsync(int id, CancellationToken ct = default);
    Task<int> ReceiveAsync(int id, CancellationToken ct = default);
    Task CancelAsync(int id, CancellationToken ct = default);
}