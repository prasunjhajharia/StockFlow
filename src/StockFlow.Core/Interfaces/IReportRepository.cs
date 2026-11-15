using StockFlow.Core.Models;

namespace StockFlow.Core.Interfaces;

public interface IReportRepository
{
    Task<IReadOnlyList<LowStockItem>> GetLowStockAsync(CancellationToken ct = default);
    Task<IReadOnlyList<WarehouseStockValue>> GetStockValueAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SalesSummaryRow>> GetSalesSummaryAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}