using Microsoft.AspNetCore.Mvc;
using StockFlow.Api.Dtos;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/warehouses")]
public class WarehousesController(IWarehouseRepository warehouses) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Warehouse>>> GetAll(CancellationToken ct)
        => Ok(await warehouses.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Warehouse>> GetById(int id, CancellationToken ct)
    {
        var warehouse = await warehouses.GetByIdAsync(id, ct);
        return warehouse is null ? NotFound() : Ok(warehouse);
    }

    [HttpPost]
    public async Task<ActionResult<Warehouse>> Create(WarehouseRequest req, CancellationToken ct)
    {
        var created = await warehouses.CreateAsync(ToWarehouse(req), ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Warehouse>> Update(int id, WarehouseRequest req, CancellationToken ct)
    {
        var warehouse = ToWarehouse(req);
        warehouse.Id = id;
        var updated = await warehouses.UpdateAsync(warehouse, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    private static Warehouse ToWarehouse(WarehouseRequest r) => new()
    {
        Name = r.Name.Trim(),
        Location = r.Location,
        IsActive = r.IsActive
    };
}