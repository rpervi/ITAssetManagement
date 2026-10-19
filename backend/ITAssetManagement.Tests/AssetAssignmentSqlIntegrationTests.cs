using System.Data;
using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Services;
using ITAssetManagement.Domain.Entities;
using ITAssetManagement.Infrastructure.Persistence;
using ITAssetManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ITAssetManagement.Tests;

[Collection("SQL Server integration tests")]
public sealed class AssetAssignmentSqlIntegrationTests
{
    [SqlServerFact]
    public async Task AssignmentsPersistIndividualAssetsAndAllocateBulkStockByFifoBatch()
    {
        var connectionString = Environment.GetEnvironmentVariable("ITAM_TEST_SQL_CONNECTION")!;
        var options = new DbContextOptionsBuilder<AssetManagementDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var context = new AssetManagementDbContext(options);
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);
        var suffix = Guid.NewGuid().ToString("N");
        var category = new AssetCategory
        {
            CategoryName = $"Assignment Integration {suffix}",
            IsActive = true,
            CreatedBy = "integration-test",
        };
        var employee = new Employee
        {
            EmployeeCode = $"EA-{suffix}",
            EmployeeName = "Assignment Integration Employee",
            Email = $"{suffix}@example.test",
            Department = "IT",
            IsActive = true,
            CreatedBy = "integration-test",
        };
        context.AssetCategories.Add(category);
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        var bulkProduct = Product(category, $"Bulk {suffix}", TrackingType.Bulk);
        var individualProduct = Product(category, $"Individual {suffix}", TrackingType.Individual);
        context.AssetProducts.AddRange(bulkProduct, individualProduct);
        await context.SaveChangesAsync();

        try
        {
            var stockService = new StockServiceImpl(new StockRepository(context));
            await stockService.ReceiveBulkAsync(
                new ReceiveBulkStockRequest(
                    Guid.NewGuid(),
                    bulkProduct.ProductId,
                    2,
                    40m,
                    new DateTime(2026, 10, 19),
                    new DateTime(2027, 10, 19),
                    "Good",
                    "Integration supplier",
                    $"BULK-FIRST-{suffix}",
                    "First FIFO batch"),
                "integration-test",
                CancellationToken.None);
            await stockService.ReceiveBulkAsync(
                new ReceiveBulkStockRequest(
                    Guid.NewGuid(),
                    bulkProduct.ProductId,
                    5,
                    45m,
                    new DateTime(2026, 10, 20),
                    new DateTime(2027, 10, 20),
                    "Good",
                    "Integration supplier",
                    $"BULK-SECOND-{suffix}",
                    "Second FIFO batch"),
                "integration-test",
                CancellationToken.None);
            await stockService.ReceiveIndividualAsync(
                new ReceiveIndividualStockRequest(
                    Guid.NewGuid(),
                    individualProduct.ProductId,
                    [
                        new IndividualStockAssetRequest(
                            $"SERIAL-{suffix}-1",
                            new DateTime(2026, 10, 19),
                            new DateTime(2027, 10, 19),
                            700m,
                            "Good"),
                        new IndividualStockAssetRequest(
                            $"SERIAL-{suffix}-2",
                            new DateTime(2026, 10, 19),
                            new DateTime(2027, 10, 19),
                            700m,
                            "Good"),
                    ],
                    "Integration supplier",
                    $"IND-{suffix}",
                    "Individual assignment assets"),
                "integration-test",
                CancellationToken.None);

            var service = new AssetAssignmentServiceImpl(new AssetAssignmentRepository(context));
            var bulkRequest = new CreateAssetAssignmentRequest(
                Guid.NewGuid(),
                employee.EmployeeId,
                bulkProduct.ProductId,
                null,
                4,
                new DateTime(2026, 10, 20),
                "Bulk assignment");
            var bulkResult = await service.CreateAsync(
                bulkRequest, "integration-test", CancellationToken.None);

            Assert.True(bulkResult.Created);
            var bulkItem = Assert.Single(bulkResult.Assignment.Items);
            Assert.Equal(4, bulkItem.Quantity);
            Assert.Equal(2, bulkItem.BatchAllocations.Count);
            Assert.Equal([2, 2], bulkItem.BatchAllocations.Select(batch => batch.Quantity));

            var replay = await service.CreateAsync(
                bulkRequest, "integration-test", CancellationToken.None);
            Assert.False(replay.Created);
            Assert.Equal(bulkResult.Assignment.AssignmentId, replay.Assignment.AssignmentId);
            await Assert.ThrowsAsync<AssetAssignmentIdempotencyConflictException>(() =>
                service.CreateAsync(
                    bulkRequest with { Quantity = 3 },
                    "integration-test",
                    CancellationToken.None));

            var availableBatches = await new AssetAssignmentRepository(context)
                .GetAvailableBatchesAsync(bulkProduct.ProductId, CancellationToken.None);
            Assert.Equal(3, availableBatches.Sum(batch => batch.AvailableQuantity));

            var unitIds = await context.AssetUnits
                .Where(unit => unit.ProductId == individualProduct.ProductId)
                .OrderBy(unit => unit.AssetUnitId)
                .Select(unit => unit.AssetUnitId)
                .ToArrayAsync();
            var individualResult = await service.CreateAsync(
                new CreateAssetAssignmentRequest(
                    Guid.NewGuid(),
                    employee.EmployeeId,
                    individualProduct.ProductId,
                    [unitIds[0]],
                    1,
                    new DateTime(2026, 10, 20),
                    "Individual assignment"),
                "integration-test",
                CancellationToken.None);
            Assert.True(individualResult.Created);
            Assert.Equal($"AST-{unitIds[0]:D6}", individualResult.Assignment.Items.Single().AssetCode);
            Assert.Equal(
                AssetStatus.Assigned,
                await context.AssetUnits.Where(unit => unit.AssetUnitId == unitIds[0])
                    .Select(unit => unit.Status)
                    .SingleAsync());
            Assert.Equal(
                1,
                await context.StockMovements.CountAsync(movement =>
                    movement.AssetUnitId == unitIds[0]
                    && movement.MovementType == MovementType.Assignment));
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        context.ChangeTracker.Clear();
        Assert.False(await context.AssetCategories.AnyAsync(
            item => item.CategoryName == category.CategoryName));
        Assert.False(await context.Employees.AnyAsync(
            item => item.EmployeeCode == employee.EmployeeCode));
    }

    private static AssetProduct Product(
        AssetCategory category,
        string name,
        TrackingType trackingType) =>
        new()
        {
            Category = category,
            CategoryId = category.CategoryId,
            ProductName = name,
            Brand = "Integration",
            Model = "Test",
            TrackingType = trackingType,
            MinimumStock = 0,
            IsActive = true,
            CreatedBy = "integration-test",
        };
}
