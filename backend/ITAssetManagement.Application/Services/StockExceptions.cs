namespace ITAssetManagement.Application.Services;

public sealed class InvalidStockRequestException(string message) : Exception(message);

public sealed class DuplicateStockIdentifierException(string message) : Exception(message);

public sealed class StockIdempotencyConflictException(string message) : Exception(message);
