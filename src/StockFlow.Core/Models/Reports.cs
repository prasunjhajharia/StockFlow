namespace StockFlow.Core.Models;

public class LowStockItem
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int ReorderLevel { get; set; }
    public int TotalQuantity { get; set; }
}

public class WarehouseStockValue
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = "";
    public int TotalUnits { get; set; }
    public decimal TotalValue { get; set; }
}

public class SalesSummaryRow
{
    public DateTime Month { get; set; }
    public int ProductId { get; set; }
    public string Sku { get; set; } = "";
    public string ProductName { get; set; } = "";
    public long UnitsSold { get; set; }
    public decimal Revenue { get; set; }
    public long RankInMonth { get; set; }
    public decimal RunningRevenue { get; set; }
}