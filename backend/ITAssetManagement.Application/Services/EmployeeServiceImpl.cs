using System.Net.Mail;
using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Domain.Entities;

namespace ITAssetManagement.Application.Services;

public sealed class EmployeeServiceImpl(IEmployeeRepository repository) : IEmployeeService
{
    public async Task<EmployeePageResponse> GetPageAsync(
        string? search,
        string? department,
        bool? isActive,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new InvalidEmployeeException("Page must be positive and page size must be between 1 and 100.");
        }

        var normalizedSortBy = NormalizeSortBy(sortBy);
        var result = await repository.GetPageAsync(
            new EmployeeListFilter(
                NormalizeOptional(search),
                NormalizeOptional(department),
                isActive,
                page,
                pageSize,
                normalizedSortBy,
                string.Equals(sortDirection?.Trim(), "desc", StringComparison.OrdinalIgnoreCase)),
            cancellationToken);
        return new EmployeePageResponse(
            result.Items.Select(ToDto).ToArray(),
            result.TotalCount,
            page,
            pageSize,
            result.Departments);
    }

    public async Task<IReadOnlyList<EmployeeOptionDto>> GetActiveAsync(
        CancellationToken cancellationToken)
    {
        var employees = await repository.GetActiveAsync(cancellationToken);
        return employees.Select(employee => new EmployeeOptionDto(
            employee.EmployeeId,
            employee.EmployeeCode,
            employee.EmployeeName,
            employee.Department)).ToArray();
    }

    public async Task<EmployeeDto?> GetByIdAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        ValidateId(employeeId);
        var employee = await repository.GetByIdAsync(employeeId, cancellationToken);
        return employee is null ? null : ToDto(employee);
    }

    public async Task<EmployeeDto> CreateAsync(
        SaveEmployeeRequest request,
        string? actor,
        CancellationToken cancellationToken)
    {
        var values = NormalizeAndValidate(request);
        await EnsureIdentifiersAreAvailableAsync(values, null, cancellationToken);

        var employee = new Employee
        {
            EmployeeCode = values.EmployeeCode,
            EmployeeName = values.EmployeeName,
            Email = values.Email,
            Department = values.Department,
            Designation = values.Designation,
            Phone = values.Phone,
            Location = values.Location,
            JoiningDate = values.JoiningDate,
            IsActive = request.IsActive,
            CreatedBy = NormalizeActor(actor),
        };
        await repository.AddAsync(employee, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(employee);
    }

    public async Task<EmployeeDto?> UpdateAsync(
        int employeeId,
        SaveEmployeeRequest request,
        string? actor,
        CancellationToken cancellationToken)
    {
        ValidateId(employeeId);
        var employee = await repository.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
        {
            return null;
        }

        var values = NormalizeAndValidate(request);
        await EnsureIdentifiersAreAvailableAsync(values, employeeId, cancellationToken);

        employee.EmployeeCode = values.EmployeeCode;
        employee.EmployeeName = values.EmployeeName;
        employee.Email = values.Email;
        employee.Department = values.Department;
        employee.Designation = values.Designation;
        employee.Phone = values.Phone;
        employee.Location = values.Location;
        employee.JoiningDate = values.JoiningDate;
        employee.IsActive = request.IsActive;
        employee.ModifiedBy = NormalizeActor(actor);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(employee);
    }

    public async Task<EmployeeDto?> SetStatusAsync(
        int employeeId,
        bool isActive,
        string? actor,
        CancellationToken cancellationToken)
    {
        ValidateId(employeeId);
        var employee = await repository.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
        {
            return null;
        }

        employee.IsActive = isActive;
        employee.ModifiedBy = NormalizeActor(actor);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(employee);
    }

    private async Task EnsureIdentifiersAreAvailableAsync(
        EmployeeValues values,
        int? excludingEmployeeId,
        CancellationToken cancellationToken)
    {
        if (await repository.EmployeeCodeExistsAsync(
                values.EmployeeCode.ToUpperInvariant(),
                excludingEmployeeId,
                cancellationToken))
        {
            throw new DuplicateEmployeeException("An employee with this employee code already exists.");
        }

        if (await repository.EmailExistsAsync(
                values.Email.ToUpperInvariant(),
                excludingEmployeeId,
                cancellationToken))
        {
            throw new DuplicateEmployeeException("An employee with this email already exists.");
        }
    }

    private static EmployeeValues NormalizeAndValidate(SaveEmployeeRequest request)
    {
        var employeeCode = Required(request.EmployeeCode, "Employee code", 50);
        var employeeName = Required(request.EmployeeName, "Employee name", 150);
        var email = Required(request.Email, "Email", 254).ToLowerInvariant();
        var department = Required(request.Department, "Department", 100);
        if (!MailAddress.TryCreate(email, out var address)
            || !string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidEmployeeException("Enter a valid email address.");
        }

        if (request.JoiningDate is { } joiningDate
            && joiningDate.Date > DateTime.UtcNow.Date)
        {
            throw new InvalidEmployeeException("Joining date cannot be in the future.");
        }

        return new EmployeeValues(
            employeeCode,
            employeeName,
            email,
            department,
            Optional(request.Designation, "Designation", 100),
            Optional(request.Phone, "Phone", 30),
            Optional(request.Location, "Location", 150),
            request.JoiningDate?.Date);
    }

    private static string Required(string? value, string field, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidEmployeeException($"{field} is required.");
        }

        if (normalized.Length > maxLength)
        {
            throw new InvalidEmployeeException($"{field} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string? Optional(string? value, string field, int maxLength)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is { Length: > 0 } && normalized.Length > maxLength)
        {
            throw new InvalidEmployeeException($"{field} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static string? NormalizeActor(string? actor)
    {
        var normalized = NormalizeOptional(actor);
        return normalized is null ? null : normalized[..Math.Min(normalized.Length, 100)];
    }

    private static string NormalizeSortBy(string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "employeecode" => "employeeCode",
            "employeename" => "employeeName",
            "email" => "email",
            "department" => "department",
            "designation" => "designation",
            "phone" => "phone",
            "location" => "location",
            "status" or "isactive" => "isActive",
            "joiningdate" => "joiningDate",
            _ => "employeeName",
        };
    }

    private static void ValidateId(int employeeId)
    {
        if (employeeId <= 0)
        {
            throw new InvalidEmployeeException("Employee ID must be positive.");
        }
    }

    private static EmployeeDto ToDto(Employee employee)
    {
        return new EmployeeDto(
            employee.EmployeeId,
            employee.EmployeeCode,
            employee.EmployeeName,
            employee.Email,
            employee.Department,
            employee.Designation,
            employee.Phone,
            employee.Location,
            employee.JoiningDate,
            employee.IsActive,
            employee.CreatedBy,
            employee.CreatedOn,
            employee.ModifiedBy,
            employee.ModifiedOn);
    }

    private sealed record EmployeeValues(
        string EmployeeCode,
        string EmployeeName,
        string Email,
        string Department,
        string? Designation,
        string? Phone,
        string? Location,
        DateTime? JoiningDate);
}
