namespace StockFlow.Core.Models;

public class SalesOrder
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int WarehouseId { get; set; }
    public string Status { get; set; } = "";
    public DateTime? ShippedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<SalesOrderLine> Lines { get; set; } = [];
}

public class SalesOrderLine
{
    public int Id { get; set; }
    public int SalesOrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}