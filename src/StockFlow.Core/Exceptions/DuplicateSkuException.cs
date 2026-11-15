namespace StockFlow.Core.Exceptions;

public class DuplicateSkuException(string sku)
    : ConflictException($"A product with SKU '{sku}' already exists.");