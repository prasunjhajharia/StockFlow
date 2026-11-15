namespace StockFlow.Core.Exceptions;

public class DuplicateWarehouseNameException(string name)
    : ConflictException($"A warehouse named '{name}' already exists.");