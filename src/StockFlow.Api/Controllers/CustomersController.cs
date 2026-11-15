using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Api.Dtos;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomersController(ICustomerRepository customers) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Customer>>> GetAll(CancellationToken ct)
        => Ok(await customers.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Customer>> GetById(int id, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(id, ct);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<Customer>> Create(ContactRequest req, CancellationToken ct)
    {
        var created = await customers.CreateAsync(ToCustomer(req), ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<Customer>> Update(int id, ContactRequest req, CancellationToken ct)
    {
        var customer = ToCustomer(req);
        customer.Id = id;
        var updated = await customers.UpdateAsync(customer, ct);
        return updated is null ? NotFound() : Ok(updated);
    }

    private static Customer ToCustomer(ContactRequest r) => new()
    {
        Name = r.Name.Trim(),
        // A blank email becomes NULL, so several customers can have no email
        Email = string.IsNullOrWhiteSpace(r.Email) ? null : r.Email.Trim(),
        Phone = string.IsNullOrWhiteSpace(r.Phone) ? null : r.Phone.Trim(),
        IsActive = r.IsActive
    };
}