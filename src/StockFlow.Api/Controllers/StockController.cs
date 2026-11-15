using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Api.Dtos;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/stock")]
[Authorize]
public class StockController(IStockRepository stock) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StockLevel>>> GetLevels(
        [FromQuery] int? warehouseId, CancellationToken ct)
        => Ok(await stock.GetLevelsAsync(warehouseId, ct));

    [HttpGet("movements/{productId:int}")]
    public async Task<ActionResult<IReadOnlyList<StockMovement>>> GetMovements(int productId, CancellationToken ct)
        => Ok(await stock.GetMovementsAsync(productId, ct));

    [HttpPost("adjust")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Adjust(StockAdjustmentRequest req, CancellationToken ct)
    {
        if (req.QuantityChange == 0)
            return BadRequest(new { error = "quantityChange cannot be 0." });

        var id = await stock.AdjustAsync(req.ProductId, req.WarehouseId, req.QuantityChange, req.Note, ct);
        return Ok(new { movementId = id });
    }
}