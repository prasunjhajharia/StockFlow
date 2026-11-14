using Dapper;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Data;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController(IDbConnectionFactory connectionFactory) : ControllerBase
{
    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase(CancellationToken ct)
    {
        await using var conn = await connectionFactory.OpenConnectionAsync(ct);
        var version = await conn.ExecuteScalarAsync<string>(
            new CommandDefinition("SELECT version();", cancellationToken: ct));

        return Ok(new { status = "ok", version });
    }
}