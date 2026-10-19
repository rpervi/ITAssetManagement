using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Application.Services;
using ITAssetManagement.Domain.Entities;

namespace ITAssetManagement.Tests;

public sealed class EmployeeServiceTests
{
    [Fact]
    public async Task CreateAsync_NormalizesValuesAndSetsAuthenticatedAuditUser()
    {
        var repository = new FakeEmployeeRepository();
        var service = new EmployeeServiceImpl(repository);

        var employee = await service.CreateAsync(
            Request(
                employeeCode: "  E-100 ",
                employeeName: "  Alex Example ",
                email: "  ALEX@example.com ",
                department: " IT "),
            " admin ",
            CancellationToken.None);

        Assert.Equal("E-100", employee.EmployeeCode);
        Assert.Equal("Alex Example", employee.EmployeeName);
        Assert.Equal("alex@example.com", employee.Email);
        Assert.Equal("IT", employee.Department);
        Assert.Equal("admin", employee.CreatedBy);
        Assert.True(employee.IsActive);
        Assert.Single(repository.Employees);
    }

    [Theory]
    [InlineData(" EMP-1 ", "emp-1")]
    [InlineData("emp-1", " EMP-1 ")]
    public async Task CreateAsync_RejectsDuplicateEmployeeCodeIgnoringCaseAndWhitespace(
        string existingCode,
        string submittedCode)
    {
        var repository = new FakeEmployeeRepository(Employee(employeeCode: existingCode));
        var service = new EmployeeServiceImpl(repository);

        await Assert.ThrowsAsync<DuplicateEmployeeException>(() =>
            service.CreateAsync(Request(employeeCode: submittedCode), "admin", CancellationToken.None));
        Assert.Single(repository.Employees);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateEmailIgnoringCaseAndWhitespace()
    {
        var repository = new FakeEmployeeRepository(Employee(email: " person@example.com "));
        var service = new EmployeeServiceImpl(repository);

        await Assert.ThrowsAsync<DuplicateEmployeeException>(() =>
            service.CreateAsync(
                Request(email: " PERSON@EXAMPLE.COM "),
                "admin",
                CancellationToken.None));
    }

    [Theory]
    [InlineData(null, "Employee code is required.")]
    [InlineData(" ", "Employee code is required.")]
    [InlineData("invalid-code-that-is-longer-than-fifty-characters-0123456789", "Employee code cannot exceed 50 characters.")]
    public async Task CreateAsync_RejectsInvalidEmployeeCode(string? code, string expectedMessage)
    {
        var service = new EmployeeServiceImpl(new FakeEmployeeRepository());

        var exception = await Assert.ThrowsAsync<InvalidEmployeeException>(() =>
            service.CreateAsync(Request(employeeCode: code), "admin", CancellationToken.None));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("name@")]
    public async Task CreateAsync_RejectsInvalidEmail(string email)
    {
        var service = new EmployeeServiceImpl(new FakeEmployeeRepository());

        await Assert.ThrowsAsync<InvalidEmployeeException>(() =>
            service.CreateAsync(Request(email: email), "admin", CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_RequiresNameDepartmentAndDoesNotAllowFutureJoiningDate()
    {
        var service = new EmployeeServiceImpl(new FakeEmployeeRepository());
        var invalidRequests = new[]
        {
            Request(employeeName: " "),
            Request(department: null),
            Request(joiningDate: DateTime.UtcNow.AddDays(1)),
        };

        foreach (var request in invalidRequests)
        {
            await Assert.ThrowsAsync<InvalidEmployeeException>(() =>
                service.CreateAsync(request, "admin", CancellationToken.None));
        }
    }

    [Fact]
    public async Task UpdateAsync_UpdatesProfileAndPreservesAssignmentHistory()
    {
        var employee = Employee();
        employee.AssetAssignments.Add(new AssetAssignment
        {
            AssignmentId = 22,
            EmployeeId = employee.EmployeeId,
            AssignedBy = "admin",
        });
        var repository = new FakeEmployeeRepository(employee);
        var service = new EmployeeServiceImpl(repository);

        var updated = await service.UpdateAsync(
            employee.EmployeeId,
            Request(employeeName: "Updated Name", isActive: false),
            " editor ",
            CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.EmployeeName);
        Assert.False(updated.IsActive);
        Assert.Equal("editor", updated.ModifiedBy);
        Assert.Single(employee.AssetAssignments);
    }

    [Fact]
    public async Task GetActiveAsyncReturnsOnlyActiveEmployees()
    {
        var repository = new FakeEmployeeRepository(Employee(), Employee(isActive: false));
        var service = new EmployeeServiceImpl(repository);

        var active = await service.GetActiveAsync(CancellationToken.None);

        Assert.Single(active);
        Assert.Equal(1, active[0].EmployeeId);
    }

    [Fact]
    public async Task SetStatusAsyncDeactivatesEmployeeWithoutDeletingAssignments()
    {
        var employee = Employee();
        employee.AssetAssignments.Add(new AssetAssignment
        {
            AssignmentId = 31,
            EmployeeId = employee.EmployeeId,
            AssignedBy = "admin",
        });
        var service = new EmployeeServiceImpl(new FakeEmployeeRepository(employee));

        var updated = await service.SetStatusAsync(
            employee.EmployeeId, false, "admin", CancellationToken.None);

        Assert.NotNull(updated);
        Assert.False(updated.IsActive);
        Assert.Single(employee.AssetAssignments);
    }

    private static SaveEmployeeRequest Request(
        string? employeeCode = "E-100",
        string? employeeName = "Alex Example",
        string? email = "alex@example.com",
        string? department = "IT",
        string? designation = null,
        string? phone = null,
        string? location = null,
        DateTime? joiningDate = null,
        bool isActive = true)
    {
        return new SaveEmployeeRequest(
            employeeCode,
            employeeName,
            email,
            department,
            designation,
            phone,
            location,
            joiningDate,
            isActive);
    }

    private static Employee Employee(
        string employeeCode = "E-100",
        string email = "alex@example.com",
        bool isActive = true)
    {
        return new Employee
        {
            EmployeeId = 1,
            EmployeeCode = employeeCode,
            EmployeeName = "Alex Example",
            Email = email,
            Department = "IT",
            IsActive = isActive,
        };
    }

    private sealed class FakeEmployeeRepository(params Employee[] employees) : IEmployeeRepository
    {
        public List<Employee> Employees { get; } = [.. employees];

        public Task<EmployeeListResult> GetPageAsync(
            EmployeeListFilter filter,
            CancellationToken cancellationToken)
        {
            var employeesQuery = Employees.AsEnumerable();
            if (filter.IsActive.HasValue)
            {
                employeesQuery = employeesQuery.Where(employee =>
                    employee.IsActive == filter.IsActive.Value);
            }

            return Task.FromResult(new EmployeeListResult(
                employeesQuery.ToArray(),
                employeesQuery.Count(),
                Employees.Select(employee => employee.Department).Distinct().ToArray()));
        }

        public Task<IReadOnlyList<Employee>> GetActiveAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Employee>>(
                Employees.Where(employee => employee.IsActive).ToArray());
        }

        public Task<Employee?> GetByIdAsync(int employeeId, CancellationToken cancellationToken)
        {
            return Task.FromResult(
                Employees.SingleOrDefault(employee => employee.EmployeeId == employeeId));
        }

        public Task<bool> EmployeeCodeExistsAsync(
            string normalizedCode,
            int? excludingEmployeeId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Employees.Any(employee =>
                employee.EmployeeId != excludingEmployeeId
                && string.Equals(
                    employee.EmployeeCode.Trim(),
                    normalizedCode,
                    StringComparison.OrdinalIgnoreCase)));
        }

        public Task<bool> EmailExistsAsync(
            string normalizedEmail,
            int? excludingEmployeeId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Employees.Any(employee =>
                employee.EmployeeId != excludingEmployeeId
                && string.Equals(
                    employee.Email.Trim(),
                    normalizedEmail,
                    StringComparison.OrdinalIgnoreCase)));
        }

        public Task AddAsync(Employee employee, CancellationToken cancellationToken)
        {
            employee.EmployeeId = Employees.Count == 0
                ? 1
                : Employees.Max(item => item.EmployeeId) + 1;
            Employees.Add(employee);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            foreach (var employee in Employees.Where(employee => employee.ModifiedBy is not null))
            {
                employee.ModifiedOn = DateTime.UtcNow;
            }

            return Task.FromResult(1);
        }
    }
}
