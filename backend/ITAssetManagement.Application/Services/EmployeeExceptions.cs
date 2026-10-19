namespace ITAssetManagement.Application.Services;

public sealed class InvalidEmployeeException(string message) : Exception(message);

public sealed class DuplicateEmployeeException(string message) : Exception(message);
