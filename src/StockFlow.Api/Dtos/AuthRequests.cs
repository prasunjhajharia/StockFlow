using System.ComponentModel.DataAnnotations;

namespace StockFlow.Api.Dtos;

public class LoginRequest
{
    [Required, StringLength(200)]
    public string Email { get; set; } = "";

    [Required, StringLength(72)]
    public string Password { get; set; } = "";
}
public class RefreshRequest
{
    [Required, StringLength(200)]
    public string RefreshToken { get; set; } = "";
}