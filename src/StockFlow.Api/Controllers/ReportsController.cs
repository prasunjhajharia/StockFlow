using Microsoft.AspNetCore.Mvc;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(IReportRepository reports) : ControllerBase
{
    [HttpGet("low-stock")]
    public async Task<ActionResult<IReadOnlyList<LowStockItem>>> LowStock(CancellationToken ct)
        => Ok(await reports.GetLowStockAsync(ct));

    [HttpGet("stock-value")]
    public async Task<ActionResult<IReadOnlyList<WarehouseStockValue>>> StockValue(CancellationToken ct)
        => Ok(await reports.GetStockValueAsync(ct));

    // Example: /api/reports/sales-summary?fromDate=2026-01-01&toDate=2026-12-31
    [HttpGet("sales-summary")]
    public async Task<ActionResult<IReadOnlyList<SalesSummaryRow>>> SalesSummary(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        var to = toDate ?? DateOnly.FromDateTime(DateTime.Today);
        var from = fromDate ?? to.AddYears(-1);

        if (from > to)
            return BadRequest(new { error = "fromDate must not be after toDate." });

        return Ok(await reports.GetSalesSummaryAsync(from, to, ct));
    }
}