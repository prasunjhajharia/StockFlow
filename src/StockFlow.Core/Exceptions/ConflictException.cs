namespace StockFlow.Core.Exceptions;

// A request is valid, but conflicts with the current state (HTTP 409)
public class ConflictException(string message) : Exception(message);