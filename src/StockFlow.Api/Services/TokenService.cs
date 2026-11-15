using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StockFlow.Api.Options;
using StockFlow.Core.Models;

namespace StockFlow.Api.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(AppUser user);
    (string Raw, string Hash, DateTime ExpiresAt) CreateRefreshToken();
    string HashToken(string rawToken);
}

public class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _jwt = options.Value;

    public (string Token, DateTime ExpiresAt) CreateAccessToken(AppUser user)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("role", user.RoleName)
            }),
            Expires = expires,
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }

    // 32 random bytes = an unguessable token. The raw value goes to the client,
    // only its hash goes to the database.
    public (string Raw, string Hash, DateTime ExpiresAt) CreateRefreshToken()
    {
        var raw = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        return (raw, HashToken(raw), DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays));
    }

    public string HashToken(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
}