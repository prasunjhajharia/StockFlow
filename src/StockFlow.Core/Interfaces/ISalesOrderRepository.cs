using StockFlow.Core.Models;

namespace StockFlow.Core.Interfaces;

public interface ISalesOrderRepository
{
    Task<IReadOnlyList<SalesOrder>> GetAllAsync(CancellationToken ct = default);
    Task<SalesOrder?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SalesOrder> CreateAsync(int customerId, int warehouseId,
        IReadOnlyList<SalesOrderLine> lines, CancellationToken ct = default);
    Task ConfirmAsync(int id, CancellationToken ct = default);
    Task<int> ShipAsync(int id, CancellationToken ct = default);
    Task CancelAsync(int id, CancellationToken ct = default);
}