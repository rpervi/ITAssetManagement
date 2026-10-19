using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Services;
using ITAssetManagement.Infrastructure.Persistence;
using ITAssetManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ITAssetManagement.Tests;

[Collection("SQL Server integration tests")]
public sealed class AssetCategorySqlIntegrationTests
{
    [SqlServerFact]
    public async Task CategoryCrudAndValidation_PersistInSqlServerAndRollbackTestData()
    {
        var connectionString = Environment.GetEnvironmentVariable("ITAM_TEST_SQL_CONNECTION")!;
        var options = new DbContextOptionsBuilder<AssetManagementDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var context = new AssetManagementDbContext(options);
        await using var transaction = await context.Database.BeginTransactionAsync();
        var service = new AssetCategoryServiceImpl(new AssetCategoryRepository(context));
        var name = $"Integration Verification {Guid.NewGuid():N}";

        try
        {
            var created = await service.CreateAsync(
                new SaveAssetCategoryRequest(name, "Temporary SQL integration data", true),
                "integration-test",
                CancellationToken.None);

            Assert.True(created.CategoryId > 0);
            Assert.Equal("integration-test", created.CreatedBy);
            Assert.Contains(
                await service.GetAllAsync(CancellationToken.None),
                category => category.CategoryId == created.CategoryId);
            Assert.Equal(created, await service.GetByIdAsync(created.CategoryId, CancellationToken.None));

            await Assert.ThrowsAsync<DuplicateAssetCategoryNameException>(() =>
                service.CreateAsync(
                    new SaveAssetCategoryRequest($"  {name.ToUpperInvariant()}  ", null),
                    "integration-test",
                    CancellationToken.None));

            var updated = await service.UpdateAsync(
                created.CategoryId,
                new SaveAssetCategoryRequest($"{name} Updated", "Updated description", false),
                "integration-test",
                CancellationToken.None);
            Assert.NotNull(updated);
            Assert.Equal("Updated description", updated.Description);
            Assert.False(updated.IsActive);
            Assert.Equal("integration-test", updated.ModifiedBy);
            Assert.NotNull(updated.ModifiedOn);

            var reactivated = await service.SetStatusAsync(
                created.CategoryId,
                true,
                "integration-test",
                CancellationToken.None);
            Assert.NotNull(reactivated);
            Assert.True(reactivated.IsActive);

            await Assert.ThrowsAsync<InvalidAssetCategoryException>(() =>
                service.CreateAsync(
                    new SaveAssetCategoryRequest("  ", null),
                    "integration-test",
                    CancellationToken.None));
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        Assert.False(await context.AssetCategories.AnyAsync(category => category.CategoryName == name));
    }
}
