using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Api.Dtos;
using StockFlow.Api.Services;
using StockFlow.Core.Interfaces;
using StockFlow.Core.Models;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    ITokenService tokens) : ControllerBase
{
    // Spends the same time on unknown emails as on real ones
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("timing-equalizer");

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        // Everyone registers as Staff. Higher roles are given by an admin.
        var user = await users.CreateAsync(email, hash, req.FullName.Trim(), "Staff", ct);
        return StatusCode(StatusCodes.Status201Created, ToProfile(user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var agent = Request.Headers.UserAgent.ToString();

        var user = await users.FindByEmailAsync(email, ct);

        if (user is null)
        {
            BCrypt.Net.BCrypt.Verify(req.Password, DummyHash);
            await users.RecordLoginAttemptAsync(email, false, ip, agent, ct);
            return InvalidCredentials();
        }

        if (user.LockedUntil is { } until && until > DateTime.UtcNow)
        {
            return StatusCode(StatusCodes.Status423Locked, new ProblemDetails
            {
                Status = StatusCodes.Status423Locked,
                Title = "Account locked",
                Detail = "Too many failed attempts. Try again later."
            });
        }

        if (!user.IsActive)
            return InvalidCredentials();

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            await users.RecordLoginAttemptAsync(email, false, ip, agent, ct);
            return InvalidCredentials();
        }

        await users.RecordLoginAttemptAsync(email, true, ip, agent, ct);
        return Ok(await IssueTokensAsync(user, ct));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest req, CancellationToken ct)
    {
        var (newRaw, newHash, newExpires) = tokens.CreateRefreshToken();

        var userId = await refreshTokens.RotateAsync(
            tokens.HashToken(req.RefreshToken), newHash, newExpires, ct);

        if (userId is null)
            return InvalidRefreshToken();

        var user = await users.FindByIdAsync(userId.Value, ct);
        if (user is null)
            return InvalidRefreshToken();

        var (access, accessExpires) = tokens.CreateAccessToken(user);
        return Ok(new
        {
            accessToken = access,
            expiresAt = accessExpires,
            refreshToken = newRaw,
            user = ToProfile(user)
        });
    }

    // The refresh token itself is the proof, so no [Authorize] is needed
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest req, CancellationToken ct)
    {
        await refreshTokens.RevokeAsync(tokens.HashToken(req.RefreshToken), ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        id = User.FindFirstValue("sub"),
        email = User.FindFirstValue("email"),
        role = User.FindFirstValue("role")
    });

    private async Task<object> IssueTokensAsync(AppUser user, CancellationToken ct)
    {
        var (access, accessExpires) = tokens.CreateAccessToken(user);
        var (raw, hash, refreshExpires) = tokens.CreateRefreshToken();
        await refreshTokens.StoreAsync(user.Id, hash, refreshExpires, ct);

        return new
        {
            accessToken = access,
            expiresAt = accessExpires,
            refreshToken = raw,
            user = ToProfile(user)
        };
    }

    private IActionResult InvalidCredentials() => Unauthorized(new ProblemDetails
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "Invalid credentials",
        Detail = "Email or password is incorrect."
    });

    private IActionResult InvalidRefreshToken() => Unauthorized(new ProblemDetails
    {
        Status = StatusCodes.Status401Unauthorized,
        Title = "Invalid refresh token",
        Detail = "Please log in again."
    });

    // Never return the password hash
    private static object ToProfile(AppUser u) => new
    {
        id = u.Id, email = u.Email, fullName = u.FullName, role = u.RoleName
    };
}