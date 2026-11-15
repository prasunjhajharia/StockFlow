using Dapper;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Data.Repositories;

public class ReportRepository(IDbConnectionFactory db) : IReportRepository
{
    public async Task<IReadOnlyList<LowStockItem>> GetLowStockAsync(CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<LowStockItem>(new CommandDefinition(
            "SELECT * FROM v_low_stock ORDER BY total_quantity, sku", cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<WarehouseStockValue>> GetStockValueAsync(CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<WarehouseStockValue>(new CommandDefinition(
            "SELECT * FROM v_stock_value ORDER BY warehouse_name", cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<IReadOnlyList<SalesSummaryRow>> GetSalesSummaryAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        await using var conn = await db.OpenConnectionAsync(ct);
        var rows = await conn.QueryAsync<SalesSummaryRow>(new CommandDefinition(
            "SELECT * FROM sales_summary(@fromDate::date, @toDate::date)",
            new
            {
                fromDate = from.ToDateTime(TimeOnly.MinValue),
                toDate = to.ToDateTime(TimeOnly.MinValue)
            },
            cancellationToken: ct));
        return rows.ToList();
    }
}