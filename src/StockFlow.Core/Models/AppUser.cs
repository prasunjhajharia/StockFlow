namespace StockFlow.Core.Models;

public class AppUser
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public string RoleName { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime? LockedUntil { get; set; }
}
public class UserSummary
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string RoleName { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}