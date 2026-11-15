using System.ComponentModel.DataAnnotations;

namespace StockFlow.Api.Dtos;

public class ContactRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [EmailAddress, StringLength(200)]
    public string? Email { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;
}