using System.ComponentModel.DataAnnotations;

namespace StockFlow.Api.Dtos;

public class ProductRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string Sku { get; set; } = "";

    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    [Range(0, 999999999)]
    public decimal UnitPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; }

    public bool IsActive { get; set; } = true;
}