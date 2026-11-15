using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Api.Dtos;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize]
public class SuppliersController(ISupplierRepository suppliers) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Supplier>>> GetAll(CancellationToken ct)
        => Ok(await suppliers.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Supplier>> GetById(int id, CancellationToken ct)
    {
        var supplier = await suppliers.GetByIdAsync(id, ct);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<Supplier>> Create(ContactRequest req, CancellationToken ct)
    {
        var created = await suppliers.CreateAsync(ToSupplier(req), ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<Supplier>> Update(int id, ContactRequest req, CancellationToken ct)
    {
        var supplier = ToSupplier(req);
        supplier.Id = id;
        var updated = await suppliers.UpdateAsync(supplier, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    private static Supplier ToSupplier(ContactRequest r) => new()
    {
        Name = r.Name.Trim(),
        Email = string.IsNullOrWhiteSpace(r.Email) ? null : r.Email.Trim(),
        Phone = string.IsNullOrWhiteSpace(r.Phone) ? null : r.Phone.Trim(),
        IsActive = r.IsActive
    };
}