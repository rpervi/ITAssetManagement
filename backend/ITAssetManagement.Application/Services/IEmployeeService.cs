using ITAssetManagement.Application.DTOs;

namespace ITAssetManagement.Application.Services;

public interface IEmployeeService
{
    Task<EmployeePageResponse> GetPageAsync(
        string? search,
        string? department,
        bool? isActive,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<EmployeeOptionDto>> GetActiveAsync(CancellationToken cancellationToken);
    Task<EmployeeDto?> GetByIdAsync(int employeeId, CancellationToken cancellationToken);
    Task<EmployeeDto> CreateAsync(
        SaveEmployeeRequest request,
        string? actor,
        CancellationToken cancellationToken);
    Task<EmployeeDto?> UpdateAsync(
        int employeeId,
        SaveEmployeeRequest request,
        string? actor,
        CancellationToken cancellationToken);
    Task<EmployeeDto?> SetStatusAsync(
        int employeeId,
        bool isActive,
        string? actor,
        CancellationToken cancellationToken);
}
