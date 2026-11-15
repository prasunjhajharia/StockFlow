using System.ComponentModel.DataAnnotations;

namespace StockFlow.Api.Dtos;

public class RegisterRequest
{
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = "";

    // BCrypt only uses the first 72 characters, so 72 is the sensible maximum
    [Required, StringLength(72, MinimumLength = 10)]
    public string Password { get; set; } = "";

    [Required, StringLength(200, MinimumLength = 1)]
    public string FullName { get; set; } = "";
}

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