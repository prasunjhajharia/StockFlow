using Dapper;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Data;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController(IDbConnectionFactory connectionFactory) : ControllerBase
{
    // No [Authorize] on purpose: monitoring tools must be able to call this without a login
    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase(CancellationToken ct)
    {
        await using var conn = await connectionFactory.OpenConnectionAsync(ct);
        await conn.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1;", cancellationToken: ct));

        return Ok(new { status = "ok" });
    }
}