using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Application.Services;
using ITAssetManagement.Domain.Entities;
using ITAssetManagement.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ITAssetManagement.Infrastructure.Repositories;

public sealed class EmployeeRepository(AssetManagementDbContext dbContext) : IEmployeeRepository
{
    public async Task<EmployeeListResult> GetPageAsync(
        EmployeeListFilter filter,
        CancellationToken cancellationToken)
    {
        var departments = await dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.Department.Trim() != string.Empty)
            .Select(employee => employee.Department)
            .Distinct()
            .OrderBy(department => department)
            .ToListAsync(cancellationToken);
        var query = dbContext.Employees.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(employee =>
                employee.EmployeeCode.Contains(search)
                || employee.EmployeeName.Contains(search)
                || employee.Email.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(filter.Department))
        {
            var department = filter.Department.Trim().ToUpper();
            query = query.Where(employee => employee.Department.ToUpper() == department);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(employee => employee.IsActive == filter.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var employees = await ApplyOrdering(query, filter.SortBy, filter.SortDescending)
            .ThenBy(employee => employee.EmployeeId)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return new EmployeeListResult(employees, totalCount, departments);
    }

    public async Task<IReadOnlyList<Employee>> GetActiveAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.IsActive)
            .OrderBy(employee => employee.EmployeeName)
            .ToListAsync(cancellationToken);
    }

    public Task<Employee?> GetByIdAsync(int employeeId, CancellationToken cancellationToken)
    {
        return dbContext.Employees
            .SingleOrDefaultAsync(employee => employee.EmployeeId == employeeId, cancellationToken);
    }

    public Task<bool> EmployeeCodeExistsAsync(
        string normalizedCode,
        int? excludingEmployeeId,
        CancellationToken cancellationToken)
    {
        return dbContext.Employees.AnyAsync(
            employee => employee.EmployeeCode.Trim().ToUpper() == normalizedCode
                && (!excludingEmployeeId.HasValue
                    || employee.EmployeeId != excludingEmployeeId.Value),
            cancellationToken);
    }

    public Task<bool> EmailExistsAsync(
        string normalizedEmail,
        int? excludingEmployeeId,
        CancellationToken cancellationToken)
    {
        return dbContext.Employees.AnyAsync(
            employee => employee.Email.Trim().ToUpper() == normalizedEmail
                && (!excludingEmployeeId.HasValue
                    || employee.EmployeeId != excludingEmployeeId.Value),
            cancellationToken);
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken)
    {
        await dbContext.Employees.AddAsync(employee, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new DuplicateEmployeeException(
                "An employee with this employee code or email already exists.");
        }
    }

    private static IOrderedQueryable<Employee> ApplyOrdering(
        IQueryable<Employee> query,
        string sortBy,
        bool descending)
    {
        return (sortBy, descending) switch
        {
            ("employeeCode", false) => query.OrderBy(employee => employee.EmployeeCode),
            ("employeeCode", true) => query.OrderByDescending(employee => employee.EmployeeCode),
            ("email", false) => query.OrderBy(employee => employee.Email),
            ("email", true) => query.OrderByDescending(employee => employee.Email),
            ("department", false) => query.OrderBy(employee => employee.Department),
            ("department", true) => query.OrderByDescending(employee => employee.Department),
            ("designation", false) => query.OrderBy(employee => employee.Designation),
            ("designation", true) => query.OrderByDescending(employee => employee.Designation),
            ("phone", false) => query.OrderBy(employee => employee.Phone),
            ("phone", true) => query.OrderByDescending(employee => employee.Phone),
            ("location", false) => query.OrderBy(employee => employee.Location),
            ("location", true) => query.OrderByDescending(employee => employee.Location),
            ("isActive", false) => query.OrderBy(employee => employee.IsActive),
            ("isActive", true) => query.OrderByDescending(employee => employee.IsActive),
            ("joiningDate", false) => query.OrderBy(employee => employee.JoiningDate),
            ("joiningDate", true) => query.OrderByDescending(employee => employee.JoiningDate),
            ("employeeName", true) => query.OrderByDescending(employee => employee.EmployeeName),
            _ => query.OrderBy(employee => employee.EmployeeName),
        };
    }
}
