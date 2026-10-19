using ITAssetManagement.Domain.Entities;
using ITAssetManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ITAssetManagement.Tests;

public sealed class AssetManagementDbContextMappingTests
{
    private static AssetManagementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AssetManagementDbContext>()
            .UseSqlServer("Server=not-used;Database=ITAssetManagementDB;Trusted_Connection=True;")
            .Options;

        return new AssetManagementDbContext(options);
    }

    [Fact]
    public void Model_MapsAllEntitiesToExpectedTables()
    {
        using var context = CreateContext();

        var expectedTables = new Dictionary<Type, string>
        {
            [typeof(AssetCategory)] = "AssetCategories",
            [typeof(AssetProduct)] = "AssetProducts",
            [typeof(AssetUnit)] = "AssetUnits",
            [typeof(StockReceipt)] = "StockReceipts",
            [typeof(StockReceiptItem)] = "StockReceiptItems",
            [typeof(Employee)] = "Employees",
            [typeof(AssetAssignment)] = "AssetAssignments",
            [typeof(AssetAssignmentItem)] = "AssetAssignmentItems",
            [typeof(AssetAssignmentBatchAllocation)] = "AssetAssignmentBatchAllocations",
            [typeof(AssetReturn)] = "AssetReturns",
            [typeof(StockMovement)] = "StockMovements",
            [typeof(StockAdjustment)] = "StockAdjustments",
        };

        foreach (var (entityType, tableName) in expectedTables)
        {
            Assert.Equal(tableName, context.Model.FindEntityType(entityType)?.GetTableName());
        }
    }

    [Fact]
    public void AssignmentModel_EnforcesIdempotencyAndReceiptBatchTraceability()
    {
        using var context = CreateContext();

        var assignment = context.Model.FindEntityType(typeof(AssetAssignment))!;
        var idempotencyIndex = Assert.Single(
            assignment.GetIndexes(),
            index => index.Properties.Single().Name == nameof(AssetAssignment.IdempotencyKey));
        var allocations = context.Model.FindEntityType(typeof(AssetAssignmentBatchAllocation))!;
        var designAllocations = context.GetService<IDesignTimeModel>()
            .Model.FindEntityType(typeof(AssetAssignmentBatchAllocation))!;
        var quantity = allocations.FindProperty(nameof(AssetAssignmentBatchAllocation.Quantity))!;
        var foreignKeys = allocations.GetForeignKeys().ToArray();

        Assert.True(idempotencyIndex.IsUnique);
        Assert.Contains("IS NOT NULL", idempotencyIndex.GetFilter());
        Assert.Equal(typeof(int), quantity.ClrType);
        Assert.Equal(2, foreignKeys.Length);
        Assert.All(foreignKeys, foreignKey =>
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        Assert.Contains(
            designAllocations.GetCheckConstraints(),
            constraint => constraint.Name == "CK_AssetAssignmentBatchAllocations_Quantity"
                && constraint.Sql == "[Quantity] > 0");
    }

    [Fact]
    public void Model_EnforcesUniqueAssetIdentifiersAndRestrictsHistoricalDeletes()
    {
        using var context = CreateContext();

        var assetUnit = context.Model.FindEntityType(typeof(AssetUnit))!;
        var assetCodeIndex = Assert.Single(
            assetUnit.GetIndexes(),
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(AssetUnit.AssetCode)]));
        var serialIndex = Assert.Single(
            assetUnit.GetIndexes(),
            index => index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(AssetUnit.SerialNumber)]));

        Assert.True(assetCodeIndex.IsUnique);
        Assert.True(serialIndex.IsUnique);
        Assert.Contains("IS NOT NULL", serialIndex.GetFilter());
        Assert.Contains("N''", serialIndex.GetFilter());

        foreach (var foreignKey in context.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
        {
            Assert.NotEqual(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
        }
    }

    [Fact]
    public void Model_UsesDecimalMoneyAndStringEnums()
    {
        using var context = CreateContext();

        var purchaseCost = context.Model.FindEntityType(typeof(AssetUnit))!
            .FindProperty(nameof(AssetUnit.PurchaseCost))!;
        var unitPrice = context.Model.FindEntityType(typeof(StockReceiptItem))!
            .FindProperty(nameof(StockReceiptItem.UnitPrice))!;
        var trackingType = context.Model.FindEntityType(typeof(AssetProduct))!
            .FindProperty(nameof(AssetProduct.TrackingType))!;

        Assert.Equal(18, purchaseCost.GetPrecision());
        Assert.Equal(2, purchaseCost.GetScale());
        Assert.Equal(18, unitPrice.GetPrecision());
        Assert.Equal(2, unitPrice.GetScale());
        Assert.Equal(typeof(string), trackingType.GetProviderClrType());
    }

    [Fact]
    public void EmployeeModel_MapsRequiredAndOptionalProfileFieldsAndPreservesAssignments()
    {
        using var context = CreateContext();
        var employee = context.Model.FindEntityType(typeof(Employee))!;
        var department = employee.FindProperty(nameof(Employee.Department))!;
        var designation = employee.FindProperty(nameof(Employee.Designation))!;
        var joiningDate = employee.FindProperty(nameof(Employee.JoiningDate))!;
        var employeeCodeIndex = Assert.Single(
            employee.GetIndexes(),
            index => index.Properties.Single().Name == nameof(Employee.EmployeeCode));
        var emailIndex = Assert.Single(
            employee.GetIndexes(),
            index => index.Properties.Single().Name == nameof(Employee.Email));
        var assignmentForeignKey = Assert.Single(
            context.Model.FindEntityType(typeof(AssetAssignment))!.GetForeignKeys(),
            foreignKey => foreignKey.Properties.Single().Name == nameof(AssetAssignment.EmployeeId));

        Assert.False(department.IsNullable);
        Assert.True(designation.IsNullable);
        Assert.True(joiningDate.IsNullable);
        Assert.Equal("date", joiningDate.GetColumnType());
        Assert.True(employeeCodeIndex.IsUnique);
        Assert.True(emailIndex.IsUnique);
        Assert.Equal(DeleteBehavior.Restrict, assignmentForeignKey.DeleteBehavior);
    }
}
