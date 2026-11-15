namespace StockFlow.Core.Models;

public class PurchaseOrder
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public int WarehouseId { get; set; }
    public string Status { get; set; } = "";
    public DateTime? OrderedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<PurchaseOrderLine> Lines { get; set; } = [];
}

public class PurchaseOrderLine
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
}