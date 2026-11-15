namespace StockFlow.Core.Models;

public class StockLevel
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public int Quantity { get; set; }
}

public class StockMovement
{
    public long Id { get; set; }
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public string MovementType { get; set; } = "";
    public int QuantityChange { get; set; }
    public string? Reference { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}