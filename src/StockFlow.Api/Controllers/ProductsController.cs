using Microsoft.AspNetCore.Mvc;
using StockFlow.Api.Dtos;
using StockFlow.Core.Exceptions;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/products")]
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
    public async Task<ActionResult<Product>> Create(ProductRequest req, CancellationToken ct)
    {
        try
        {
            var created = await products.CreateAsync(ToProduct(req), ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DuplicateSkuException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Product>> Update(int id, ProductRequest req, CancellationToken ct)
    {
        try
        {
            var product = ToProduct(req);
            product.Id = id;
            var updated = await products.UpdateAsync(product, ct);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (DuplicateSkuException ex)
        {
            return Conflict(new { error = ex.Message });
        }
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