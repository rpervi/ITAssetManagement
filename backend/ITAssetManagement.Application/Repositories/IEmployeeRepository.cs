using ITAssetManagement.Domain.Entities;

namespace ITAssetManagement.Application.Repositories;

public sealed record EmployeeListFilter(
    string? Search,
    string? Department,
    bool? IsActive,
    int Page,
    int PageSize,
    string SortBy,
    bool SortDescending);

public sealed record EmployeeListResult(
    IReadOnlyList<Employee> Items,
    int TotalCount,
    IReadOnlyList<string> Departments);

public interface IEmployeeRepository
{
    Task<EmployeeListResult> GetPageAsync(
        EmployeeListFilter filter,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Employee>> GetActiveAsync(CancellationToken cancellationToken);
    Task<Employee?> GetByIdAsync(int employeeId, CancellationToken cancellationToken);
    Task<bool> EmployeeCodeExistsAsync(
        string normalizedCode,
        int? excludingEmployeeId,
        CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(
        string normalizedEmail,
        int? excludingEmployeeId,
        CancellationToken cancellationToken);
    Task AddAsync(Employee employee, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
