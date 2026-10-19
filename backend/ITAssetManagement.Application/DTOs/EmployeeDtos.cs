using System.Text.Json.Serialization;

namespace ITAssetManagement.Application.DTOs;

public sealed record EmployeeDto(
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string Email,
    string Department,
    string? Designation,
    string? Phone,
    string? Location,
    DateTime? JoiningDate,
    bool IsActive,
    string? CreatedBy,
    DateTime CreatedOn,
    string? ModifiedBy,
    DateTime? ModifiedOn);

public sealed record EmployeeOptionDto(
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string Department);

public sealed record SaveEmployeeRequest(
    string? EmployeeCode,
    string? EmployeeName,
    string? Email,
    string? Department,
    string? Designation,
    string? Phone,
    string? Location,
    DateTime? JoiningDate,
    bool IsActive = true);

public sealed record EmployeeStatusRequest([property: JsonRequired] bool IsActive);

public sealed record EmployeePageResponse(
    IReadOnlyList<EmployeeDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<string> Departments);
