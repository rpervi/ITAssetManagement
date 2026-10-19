namespace ITAssetManagement.Application.Services;

public sealed class InvalidAssetAssignmentException(string message) : Exception(message);

public sealed class AssetAssignmentConflictException(string message) : Exception(message);

public sealed class AssetAssignmentIdempotencyConflictException(string message) : Exception(message);
