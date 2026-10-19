using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Services;
using ITAssetManagement.Domain.Entities;
using ITAssetManagement.Infrastructure.Persistence;
using ITAssetManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ITAssetManagement.Tests;

[Collection("SQL Server integration tests")]
public sealed class StockSqlIntegrationTests
{
    [SqlServerFact]
    public async Task StockReceiptsPersistIndividualAndBulkInventoryAndRollback()
    {
        var connectionString = Environment.GetEnvironmentVariable("ITAM_TEST_SQL_CONNECTION")!;
        var options = new DbContextOptionsBuilder<AssetManagementDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var context = new AssetManagementDbContext(options);
        await using var transaction = await context.Database.BeginTransactionAsync();
        var suffix = Guid.NewGuid().ToString("N");
        var category = new AssetCategory
        {
            CategoryName = $"Stock Integration {suffix}",
            IsActive = true,
            CreatedBy = "integration-test",
        };
        context.AssetCategories.Add(category);
        await context.SaveChangesAsync();
        var individualProduct = Product(category, $"Individual {suffix}", TrackingType.Individual);
        var bulkProduct = Product(category, $"Bulk {suffix}", TrackingType.Bulk);
        var inactiveProduct = Product(category, $"Inactive {suffix}", TrackingType.Bulk, isActive: false);
        context.AssetProducts.AddRange(individualProduct, bulkProduct, inactiveProduct);
        await context.SaveChangesAsync();
        var service = new StockServiceImpl(new StockRepository(context));
        var serialNumber = $"SERIAL-{suffix}";

        try
        {
            var individualRequest = new ReceiveIndividualStockRequest(
                Guid.NewGuid(),
                individualProduct.ProductId,
                [
                    IndividualAsset(serialNumber, "Good"),
                    IndividualAsset(null, "Damaged"),
                ],
                "Integration supplier",
                $"INV-{suffix}",
                "Individual integration receipt");
            var individualReceipt = await service.ReceiveIndividualAsync(
                individualRequest, "integration-test", CancellationToken.None);

            Assert.True(individualReceipt.Created);
            Assert.Equal(2, individualReceipt.Receipt.Quantity);
            Assert.Equal(2, await context.AssetUnits.CountAsync(
                unit => unit.ProductId == individualProduct.ProductId));
            Assert.Equal(2, await context.StockMovements.CountAsync(
                movement => movement.ReferenceId == individualReceipt.Receipt.ReceiptId));
            Assert.Equal(
                1,
                (await service.GetAvailabilityAsync(
                    individualProduct.ProductId, CancellationToken.None))?.AvailableQuantity);
            Assert.Equal(
                1,
                (await service.GetAvailabilityAsync(
                    individualProduct.ProductId, CancellationToken.None))?.DamagedQuantity);

            await Assert.ThrowsAsync<DuplicateStockIdentifierException>(() =>
                service.ReceiveIndividualAsync(
                    individualRequest with
                    {
                        IdempotencyKey = Guid.NewGuid(),
                        Assets = [IndividualAsset($"  {serialNumber.ToLowerInvariant()} ", "Good")],
                    },
                    "integration-test",
                    CancellationToken.None));

            var firstBulk = new ReceiveBulkStockRequest(
                Guid.NewGuid(),
                bulkProduct.ProductId,
                25,
                1200m,
                new DateTime(2026, 10, 19),
                new DateTime(2027, 10, 19),
                "Good",
                "Integration supplier",
                $"BULK-{suffix}",
                "Bulk receipt");
            var firstReceipt = await service.ReceiveBulkAsync(
                firstBulk, "integration-test", CancellationToken.None);
            var replayedReceipt = await service.ReceiveBulkAsync(
                firstBulk, "integration-test", CancellationToken.None);
            Assert.True(firstReceipt.Created);
            Assert.False(replayedReceipt.Created);
            Assert.Equal(firstReceipt.Receipt.ReceiptId, replayedReceipt.Receipt.ReceiptId);

            var secondReceipt = await service.ReceiveBulkAsync(
                firstBulk with { IdempotencyKey = Guid.NewGuid(), Quantity = 10 },
                "integration-test",
                CancellationToken.None);
            Assert.Equal(25, firstReceipt.Receipt.Quantity);
            Assert.Equal(10, secondReceipt.Receipt.Quantity);
            Assert.Equal(
                35,
                (await service.GetAvailabilityAsync(
                    bulkProduct.ProductId, CancellationToken.None))?.AvailableQuantity);
            Assert.Equal(
                0,
                await context.AssetUnits.CountAsync(unit => unit.ProductId == bulkProduct.ProductId));
            Assert.Equal(
                2,
                await context.StockMovements.CountAsync(
                    movement => movement.ProductId == bulkProduct.ProductId));

            await Assert.ThrowsAsync<InvalidStockRequestException>(() =>
                service.ReceiveBulkAsync(
                    firstBulk with { IdempotencyKey = Guid.NewGuid(), Quantity = 0 },
                    "integration-test",
                    CancellationToken.None));
            await Assert.ThrowsAsync<InvalidStockRequestException>(() =>
                service.ReceiveBulkAsync(
                    firstBulk with { IdempotencyKey = Guid.NewGuid(), UnitPrice = -1 },
                    "integration-test",
                    CancellationToken.None));
            await Assert.ThrowsAsync<InvalidStockRequestException>(() =>
                service.ReceiveBulkAsync(
                    firstBulk with
                    {
                        IdempotencyKey = Guid.NewGuid(),
                        PurchaseDate = new DateTime(2026, 10, 19),
                        WarrantyExpiryDate = new DateTime(2026, 10, 18),
                    },
                    "integration-test",
                    CancellationToken.None));
            await Assert.ThrowsAsync<InvalidStockRequestException>(() =>
                service.ReceiveBulkAsync(
                    firstBulk with
                    {
                        IdempotencyKey = Guid.NewGuid(),
                        ProductId = inactiveProduct.ProductId,
                    },
                    "integration-test",
                    CancellationToken.None));

            var history = await service.GetReceiptsAsync(
                search: suffix,
                productId: bulkProduct.ProductId,
                startDate: new DateTime(2026, 10, 19),
                endDate: new DateTime(2026, 10, 19),
                page: 1,
                pageSize: 10,
                sortBy: "receivedDate",
                sortDirection: "desc",
                CancellationToken.None);
            Assert.Equal(2, history.TotalCount);
            Assert.All(history.Items, item => Assert.Equal("integration-test", item.ReceivedBy));
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        context.ChangeTracker.Clear();
        Assert.False(await context.AssetProducts.AnyAsync(
            item => item.ProductName == individualProduct.ProductName
                || item.ProductName == bulkProduct.ProductName
                || item.ProductName == inactiveProduct.ProductName));
        Assert.False(await context.AssetCategories.AnyAsync(
            item => item.CategoryName == category.CategoryName));
    }

    private static AssetProduct Product(
        AssetCategory category,
        string name,
        TrackingType trackingType,
        bool isActive = true)
    {
        return new AssetProduct
        {
            CategoryId = category.CategoryId,
            Category = category,
            ProductName = name,
            Brand = "Integration",
            Model = "Test",
            TrackingType = trackingType,
            MinimumStock = 0,
            IsActive = isActive,
            CreatedBy = "integration-test",
        };
    }

    private static IndividualStockAssetRequest IndividualAsset(
        string? serialNumber,
        string condition)
    {
        return new IndividualStockAssetRequest(
            serialNumber,
            new DateTime(2026, 10, 19),
            new DateTime(2027, 10, 19),
            65000m,
            condition);
    }
}
