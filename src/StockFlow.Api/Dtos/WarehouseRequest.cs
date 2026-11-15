using System.ComponentModel.DataAnnotations;

namespace StockFlow.Api.Dtos;

public class WarehouseRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [StringLength(200)]
    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;
}