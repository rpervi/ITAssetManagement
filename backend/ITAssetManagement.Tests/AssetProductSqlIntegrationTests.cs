using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Services;
using ITAssetManagement.Domain.Entities;
using ITAssetManagement.Infrastructure.Persistence;
using ITAssetManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ITAssetManagement.Tests;

[Collection("SQL Server integration tests")]
public sealed class AssetProductSqlIntegrationTests
{
    [SqlServerFact]
    public async Task ProductCrudAndValidation_PersistInSqlServerAndRollbackTestData()
    {
        var connectionString = Environment.GetEnvironmentVariable("ITAM_TEST_SQL_CONNECTION")!;
        var options = new DbContextOptionsBuilder<AssetManagementDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var context = new AssetManagementDbContext(options);
        await using var transaction = await context.Database.BeginTransactionAsync();
        var category = new AssetCategory
        {
            CategoryName = $"Product Integration {Guid.NewGuid():N}",
            IsActive = true,
            CreatedBy = "integration-test",
        };
        context.AssetCategories.Add(category);
        await context.SaveChangesAsync();
        var service = new AssetProductServiceImpl(new AssetProductRepository(context));
        var productName = $"Product {Guid.NewGuid():N}";

        try
        {
            var created = await service.CreateAsync(
                Request(category.CategoryId, productName),
                "integration-test",
                CancellationToken.None);

            Assert.True(created.ProductId > 0);
            Assert.Equal(category.CategoryName, created.CategoryName);
            Assert.Equal("integration-test", created.CreatedBy);
            Assert.Contains(
                await service.GetAllAsync(CancellationToken.None),
                product => product.ProductId == created.ProductId);
            Assert.Equal(created, await service.GetByIdAsync(created.ProductId, CancellationToken.None));

            await Assert.ThrowsAsync<DuplicateAssetProductException>(() =>
                service.CreateAsync(
                    Request(category.CategoryId, $" {productName.ToUpperInvariant()} ", " ACME ", " x1 "),
                    "integration-test",
                    CancellationToken.None));
            await Assert.ThrowsAsync<InvalidAssetProductException>(() =>
                service.CreateAsync(
                    Request(int.MaxValue, $"{productName} other"),
                    "integration-test",
                    CancellationToken.None));
            await Assert.ThrowsAsync<InvalidAssetProductException>(() =>
                service.CreateAsync(
                    Request(category.CategoryId, $"{productName} invalid tracking", trackingType: "None"),
                    "integration-test",
                    CancellationToken.None));
            await Assert.ThrowsAsync<InvalidAssetProductException>(() =>
                service.CreateAsync(
                    Request(category.CategoryId, $"{productName} negative", minimumStock: -1),
                    "integration-test",
                    CancellationToken.None));

            var updated = await service.UpdateAsync(
                created.ProductId,
                Request(category.CategoryId, $"{productName} Updated", trackingType: "Bulk"),
                "integration-test",
                CancellationToken.None);
            Assert.NotNull(updated);
            Assert.Equal("Bulk", updated.TrackingType);
            Assert.Equal("integration-test", updated.ModifiedBy);
            Assert.NotNull(updated.ModifiedOn);

            context.StockAdjustments.Add(new StockAdjustment
            {
                ProductId = created.ProductId,
                QuantityChange = 1,
                Reason = "Integration test history",
                AdjustmentDate = DateTime.UtcNow,
                ApprovedBy = "integration-test",
                CreatedBy = "integration-test",
            });
            await context.SaveChangesAsync();

            await Assert.ThrowsAsync<AssetProductHistoryException>(() =>
                service.UpdateAsync(
                    created.ProductId,
                    Request(category.CategoryId, $"{productName} Updated", trackingType: "Individual"),
                    "integration-test",
                    CancellationToken.None));

            var deactivated = await service.SetStatusAsync(
                created.ProductId, false, "integration-test", CancellationToken.None);
            Assert.NotNull(deactivated);
            Assert.False(deactivated.IsActive);
            Assert.Equal(
                created.ProductId,
                (await service.GetByIdAsync(created.ProductId, CancellationToken.None))?.ProductId);
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        Assert.False(await context.AssetProducts.AnyAsync(product => product.ProductName == productName));
        Assert.False(await context.AssetCategories.AnyAsync(item => item.CategoryName == category.CategoryName));
    }

    private static SaveAssetProductRequest Request(
        int categoryId,
        string productName,
        string brand = "Acme",
        string? model = "X1",
        string trackingType = "Individual",
        int minimumStock = 0,
        bool isActive = true)
    {
        return new SaveAssetProductRequest(
            categoryId, productName, brand, model, trackingType, minimumStock, isActive);
    }
}
