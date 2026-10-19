namespace ITAssetManagement.Application.Services;

public sealed class DuplicateAssetProductException(string message) : Exception(message);

public sealed class InvalidAssetProductException(string message) : Exception(message);

public sealed class AssetProductHistoryException(string message) : Exception(message);
