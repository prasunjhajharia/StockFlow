using System.ComponentModel.DataAnnotations;

namespace StockFlow.Api.Dtos;

public class PurchaseOrderRequest
{
    [Range(1, int.MaxValue)] public int SupplierId { get; set; }
    [Range(1, int.MaxValue)] public int WarehouseId { get; set; }

    [Required, MinLength(1)]
    public List<PurchaseOrderLineRequest> Lines { get; set; } = [];
}

public class PurchaseOrderLineRequest
{
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, 1000000)] public int Quantity { get; set; }
    [Range(0, 999999999)] public decimal UnitCost { get; set; }
}