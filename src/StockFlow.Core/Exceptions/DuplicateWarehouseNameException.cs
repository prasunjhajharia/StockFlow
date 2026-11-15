namespace StockFlow.Core.Exceptions;

public class DuplicateWarehouseNameException(string name)
    : Exception($"A warehouse named '{name}' already exists.");