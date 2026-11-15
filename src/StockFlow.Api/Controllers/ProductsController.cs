using Microsoft.AspNetCore.Mvc;
using StockFlow.Api.Dtos;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;
using Microsoft.AspNetCore.Authorization;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController(IProductRepository products) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Product>>> GetAll(CancellationToken ct)
        => Ok(await products.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetById(int id, CancellationToken ct)
    {
        var product = await products.GetByIdAsync(id, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<Product>> Create(ProductRequest req, CancellationToken ct)
    {
        var created = await products.CreateAsync(ToProduct(req), ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<Product>> Update(int id, ProductRequest req, CancellationToken ct)
    {
        var product = ToProduct(req);
        product.Id = id;
        var updated = await products.UpdateAsync(product, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    private static Product ToProduct(ProductRequest r) => new()
    {
        Sku = r.Sku.Trim(),
        Name = r.Name.Trim(),
        Description = r.Description,
        UnitPrice = r.UnitPrice,
        ReorderLevel = r.ReorderLevel,
        IsActive = r.IsActive
    };
}