using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Api.Dtos;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController(IUserRepository users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserSummary>>> GetAll(CancellationToken ct)
        => Ok(await users.GetAllAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest req, CancellationToken ct)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        var user = await users.CreateAsync(
            req.Email.Trim().ToLowerInvariant(), hash, req.FullName.Trim(), req.Role, ct);

        return StatusCode(StatusCodes.Status201Created, new
        {
            id = user.Id, email = user.Email, fullName = user.FullName, role = user.RoleName
        });
    }

    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> ChangeRole(int id, ChangeRoleRequest req, CancellationToken ct)
    {
        await users.ChangeRoleAsync(CurrentUserId(), id, req.Role, ct);
        return NoContent();
    }

    [HttpPut("{id:int}/active")]
    public async Task<IActionResult> SetActive(int id, SetActiveRequest req, CancellationToken ct)
    {
        await users.SetActiveAsync(CurrentUserId(), id, req.IsActive, ct);
        return NoContent();
    }

    private int CurrentUserId() => int.Parse(User.FindFirst("sub")!.Value);
}