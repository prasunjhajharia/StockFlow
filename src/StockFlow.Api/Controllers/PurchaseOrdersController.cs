using Microsoft.AspNetCore.Mvc;
using StockFlow.Api.Dtos;
using StockFlow.Core.Exceptions;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/purchase-orders")]
public class PurchaseOrdersController(IPurchaseOrderRepository orders) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrder>>> GetAll(CancellationToken ct)
        => Ok(await orders.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PurchaseOrder>> GetById(int id, CancellationToken ct)
    {
        var po = await orders.GetByIdAsync(id, ct);
        return po is null ? NotFound() : Ok(po);
    }

    [HttpPost]
    public async Task<ActionResult<PurchaseOrder>> Create(PurchaseOrderRequest req, CancellationToken ct)
    {
        var lines = req.Lines.Select(l => new PurchaseOrderLine
        {
            ProductId = l.ProductId, Quantity = l.Quantity, UnitCost = l.UnitCost
        }).ToList();

        var created = await orders.CreateAsync(req.SupplierId, req.WarehouseId, lines, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:int}/order")]
    public async Task<ActionResult<PurchaseOrder>> MarkOrdered(int id, CancellationToken ct)
    {
        await orders.MarkOrderedAsync(id, ct);
        return Ok(await orders.GetByIdAsync(id, ct));
    }

    [HttpPost("{id:int}/receive")]
    public async Task<IActionResult> Receive(int id, CancellationToken ct)
    {
        var count = await orders.ReceiveAsync(id, ct);
        return Ok(new { linesReceived = count });
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<PurchaseOrder>> Cancel(int id, CancellationToken ct)
    {
        await orders.CancelAsync(id, ct);
        return Ok(await orders.GetByIdAsync(id, ct));
    }
}