using System.ComponentModel.DataAnnotations;

namespace StockFlow.Api.Dtos;

public class SalesOrderRequest
{
    [Range(1, int.MaxValue)] public int CustomerId { get; set; }
    [Range(1, int.MaxValue)] public int WarehouseId { get; set; }

    [Required, MinLength(1)]
    public List<SalesOrderLineRequest> Lines { get; set; } = [];
}

public class SalesOrderLineRequest
{
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, 1000000)] public int Quantity { get; set; }
}