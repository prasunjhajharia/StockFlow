using System.ComponentModel.DataAnnotations;

namespace StockFlow.Api.Dtos;

public class StockAdjustmentRequest
{
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, int.MaxValue)] public int WarehouseId { get; set; }

    // positive = add, negative = remove (0 is not allowed)
    [Range(-1000000, 1000000)] public int QuantityChange { get; set; }

    [StringLength(500)] public string? Note { get; set; }
}