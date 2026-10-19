namespace ITAssetManagement.Application.Services;

public sealed class DuplicateAssetCategoryNameException(string message) : Exception(message);

public sealed class InvalidAssetCategoryException(string message) : Exception(message);
