using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Services;
using ITAssetManagement.Infrastructure.Persistence;
using ITAssetManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ITAssetManagement.Tests;

[Collection("SQL Server integration tests")]
public sealed class EmployeeSqlIntegrationTests
{
    [SqlServerFact]
    public async Task EmployeeManagementPersistsProfilesAndPreservesAssignmentHistory()
    {
        var connectionString = Environment.GetEnvironmentVariable("ITAM_TEST_SQL_CONNECTION")!;
        var options = new DbContextOptionsBuilder<AssetManagementDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var context = new AssetManagementDbContext(options);
        await using var transaction = await context.Database.BeginTransactionAsync();
        var suffix = Guid.NewGuid().ToString("N");
        var service = new EmployeeServiceImpl(new EmployeeRepository(context));
        var request = new SaveEmployeeRequest(
            $"EMP-{suffix}",
            "Integration Employee",
            $"{suffix}@example.test",
            "IT Integration",
            "Test Analyst",
            "555-0100",
            "Development",
            DateTime.UtcNow.Date.AddDays(-3),
            true);

        try
        {
            var created = await service.CreateAsync(request, "integration-test", CancellationToken.None);
            Assert.True(created.EmployeeId > 0);
            Assert.Equal("integration-test", created.CreatedBy);
            Assert.Equal(request.EmployeeCode, created.EmployeeCode);
            Assert.Equal(request.Email, created.Email);
            Assert.Equal(DateTime.UtcNow.Date.AddDays(-3), created.JoiningDate?.Date);
            Assert.True(await context.Employees.AnyAsync(
                employee => employee.EmployeeId == created.EmployeeId));

            var byId = await service.GetByIdAsync(created.EmployeeId, CancellationToken.None);
            Assert.Equal(created.EmployeeId, byId?.EmployeeId);

            var list = await service.GetPageAsync(
                suffix,
                "IT Integration",
                true,
                1,
                10,
                "employeeCode",
                "asc",
                CancellationToken.None);
            Assert.Equal(1, list.TotalCount);
            Assert.Equal(created.EmployeeId, Assert.Single(list.Items).EmployeeId);
            Assert.Contains("IT Integration", list.Departments);

            await Assert.ThrowsAsync<DuplicateEmployeeException>(() =>
                service.CreateAsync(
                    request with { EmployeeCode = $" {request.EmployeeCode!.ToLowerInvariant()} " },
                    "integration-test",
                    CancellationToken.None));
            await Assert.ThrowsAsync<DuplicateEmployeeException>(() =>
                service.CreateAsync(
                    request with
                    {
                        EmployeeCode = $"OTHER-{suffix}",
                        Email = request.Email!.ToUpperInvariant(),
                    },
                    "integration-test",
                    CancellationToken.None));
            await Assert.ThrowsAsync<InvalidEmployeeException>(() =>
                service.CreateAsync(
                    request with { Email = "not-an-email" },
                    "integration-test",
                    CancellationToken.None));
            await Assert.ThrowsAsync<InvalidEmployeeException>(() =>
                service.CreateAsync(
                    request with { Department = " " },
                    "integration-test",
                    CancellationToken.None));

            var assignment = new ITAssetManagement.Domain.Entities.AssetAssignment
            {
                EmployeeId = created.EmployeeId,
                AssignedBy = "integration-test",
            };
            context.AssetAssignments.Add(assignment);
            await context.SaveChangesAsync();

            var updated = await service.UpdateAsync(
                created.EmployeeId,
                request with { EmployeeName = "Updated Integration Employee" },
                "integration-editor",
                CancellationToken.None);
            Assert.Equal("Updated Integration Employee", updated?.EmployeeName);
            Assert.Equal("integration-editor", updated?.ModifiedBy);

            var deactivated = await service.SetStatusAsync(
                created.EmployeeId, false, "integration-admin", CancellationToken.None);
            Assert.False(deactivated?.IsActive);
            Assert.Empty(await service.GetActiveAsync(CancellationToken.None));
            Assert.True(await context.AssetAssignments.AnyAsync(
                item => item.AssignmentId == assignment.AssignmentId));

            var inactiveList = await service.GetPageAsync(
                suffix,
                null,
                false,
                1,
                10,
                "employeeName",
                "desc",
                CancellationToken.None);
            Assert.Equal(created.EmployeeId, Assert.Single(inactiveList.Items).EmployeeId);
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        context.ChangeTracker.Clear();
        Assert.False(await context.Employees.AnyAsync(
            employee => employee.EmployeeCode == request.EmployeeCode));
    }
}
