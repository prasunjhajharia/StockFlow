using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StockFlow.Api.Dtos;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/sales-orders")]
public class SalesOrdersController(ISalesOrderRepository orders) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SalesOrder>>> GetAll(CancellationToken ct)
        => Ok(await orders.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SalesOrder>> GetById(int id, CancellationToken ct)
    {
        var so = await orders.GetByIdAsync(id, ct);
        return so is null ? NotFound() : Ok(so);
    }

    [HttpPost]
    public async Task<ActionResult<SalesOrder>> Create(SalesOrderRequest req, CancellationToken ct)
    {
        var lines = req.Lines.Select(l => new SalesOrderLine
        {
            ProductId = l.ProductId, Quantity = l.Quantity
        }).ToList();

        var created = await orders.CreateAsync(req.CustomerId, req.WarehouseId, lines, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:int}/confirm")]
    public async Task<ActionResult<SalesOrder>> Confirm(int id, CancellationToken ct)
    {
        await orders.ConfirmAsync(id, ct);
        return Ok(await orders.GetByIdAsync(id, ct));
    }

    [HttpPost("{id:int}/ship")]
    public async Task<IActionResult> Ship(int id, CancellationToken ct)
    {
        var count = await orders.ShipAsync(id, ct);
        return Ok(new { linesShipped = count });
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<SalesOrder>> Cancel(int id, CancellationToken ct)
    {
        await orders.CancelAsync(id, ct);
        return Ok(await orders.GetByIdAsync(id, ct));
    }
}