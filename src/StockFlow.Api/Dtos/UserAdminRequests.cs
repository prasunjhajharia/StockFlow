using System.ComponentModel.DataAnnotations;

namespace StockFlow.Api.Dtos;

public class CreateUserRequest
{
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = "";

    [Required, StringLength(72, MinimumLength = 10)]
    public string Password { get; set; } = "";

    [Required, StringLength(200, MinimumLength = 1)]
    public string FullName { get; set; } = "";

    [Required, RegularExpression("^(Admin|Manager|Staff)$")]
    public string Role { get; set; } = "Staff";
}

public class ChangeRoleRequest
{
    [Required, RegularExpression("^(Admin|Manager|Staff)$")]
    public string Role { get; set; } = "";
}

public class SetActiveRequest
{
    public bool IsActive { get; set; }
}