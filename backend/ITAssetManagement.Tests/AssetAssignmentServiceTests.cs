using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Application.Services;
using ITAssetManagement.Domain.Entities;

namespace ITAssetManagement.Tests;

public sealed class AssetAssignmentServiceTests
{
    [Fact]
    public async Task CreateAsync_IndividualAssignmentCreatesOneItemPerSelectedUnit()
    {
        var repository = new FakeAssetAssignmentRepository(TrackingType.Individual);
        var service = new AssetAssignmentServiceImpl(repository);
        var request = Request(
            assetUnitIds: [12, 14],
            quantity: 2);

        var result = await service.CreateAsync(request, "it-staff", CancellationToken.None);

        Assert.True(result.Created);
        Assert.Equal(2, repository.SavedAssignment!.Items.Count);
        Assert.All(repository.SavedAssignment.Items, item =>
        {
            Assert.Equal(1, item.Quantity);
            Assert.Equal(5, item.ProductId);
        });
        Assert.Equal("it-staff", repository.SavedAssignment.CreatedBy);
        Assert.Equal(DateTimeKind.Utc, repository.SavedAssignment.AssignedDate.Kind);
    }

    [Fact]
    public async Task CreateAsync_BulkAssignmentStoresRequestedQuantityAsSingleLine()
    {
        var repository = new FakeAssetAssignmentRepository(TrackingType.Bulk);
        var service = new AssetAssignmentServiceImpl(repository);

        var result = await service.CreateAsync(
            Request(assetUnitIds: [], quantity: 7),
            "it-staff",
            CancellationToken.None);

        var item = Assert.Single(repository.SavedAssignment!.Items);
        Assert.Equal(7, item.Quantity);
        Assert.Null(item.AssetUnitId);
        Assert.Equal(7, result.Assignment.IssuedQuantity);
    }

    [Fact]
    public async Task CreateAsync_RejectsEmptyIdempotencyKey()
    {
        var repository = new FakeAssetAssignmentRepository(TrackingType.Bulk);
        var service = new AssetAssignmentServiceImpl(repository);

        await Assert.ThrowsAsync<InvalidAssetAssignmentException>(() =>
            service.CreateAsync(
                Request(idempotencyKey: Guid.Empty, assetUnitIds: null),
                "it-staff",
                CancellationToken.None));

        Assert.Null(repository.SavedAssignment);
    }

    [Fact]
    public async Task CreateAsync_RejectsBulkAssignmentWithoutPositiveQuantity()
    {
        var repository = new FakeAssetAssignmentRepository(TrackingType.Bulk);
        var service = new AssetAssignmentServiceImpl(repository);

        await Assert.ThrowsAsync<InvalidAssetAssignmentException>(() =>
            service.CreateAsync(
                Request(assetUnitIds: [], quantity: 0),
                "it-staff",
                CancellationToken.None));

        Assert.Null(repository.SavedAssignment);
    }

    [Fact]
    public async Task CreateAsync_RejectsInactiveEmployee()
    {
        var repository = new FakeAssetAssignmentRepository(TrackingType.Bulk)
        {
            Employee = new Employee
            {
                EmployeeCode = "E-1",
                EmployeeName = "Test Employee",
                Email = "employee@example.test",
                Department = "IT",
                IsActive = false,
            },
        };
        var service = new AssetAssignmentServiceImpl(repository);

        await Assert.ThrowsAsync<InvalidAssetAssignmentException>(() =>
            service.CreateAsync(Request(assetUnitIds: []), "it-staff", CancellationToken.None));

        Assert.Null(repository.SavedAssignment);
    }

    [Fact]
    public async Task ReturnAsync_RecordsPartialReturnAndReturnedCondition()
    {
        var repository = new FakeAssetAssignmentRepository(TrackingType.Individual);
        var service = new AssetAssignmentServiceImpl(repository);
        var assignment = await service.CreateAsync(
            Request(assetUnitIds: [12, 14], quantity: 2),
            "it-staff",
            CancellationToken.None);

        var result = await service.ReturnAsync(
            assignment.Assignment.AssignmentId,
            new ReturnAssetAssignmentRequest(
                assignment.Assignment.Items[0].AssignmentItemId,
                1,
                new DateTime(2026, 10, 21),
                "Damaged",
                "Cracked casing"),
            "it-staff",
            CancellationToken.None);

        Assert.Equal("Partially Returned", result.Status);
        Assert.Equal(1, result.Items[0].ReturnedQuantity);
        Assert.Equal(0, result.Items[0].OutstandingQuantity);
        Assert.Equal(AssetCondition.Damaged, repository.ReturnedCondition);
    }

    [Fact]
    public async Task ReturnAsync_RejectsUnknownReturnedCondition()
    {
        var repository = new FakeAssetAssignmentRepository(TrackingType.Individual);
        var service = new AssetAssignmentServiceImpl(repository);

        await Assert.ThrowsAsync<InvalidAssetAssignmentException>(() =>
            service.ReturnAsync(
                42,
                new ReturnAssetAssignmentRequest(1, 1, new DateTime(2026, 10, 21), "Unknown", null),
                "it-staff",
                CancellationToken.None));

        Assert.Null(repository.ReturnedCondition);
    }

    private static CreateAssetAssignmentRequest Request(
        Guid? idempotencyKey = null,
        IReadOnlyList<int>? assetUnitIds = null,
        int quantity = 1) =>
        new(
            idempotencyKey ?? Guid.NewGuid(),
            8,
            5,
            assetUnitIds ?? [12],
            quantity,
            new DateTime(2026, 10, 19),
            "Test assignment");

    private sealed class FakeAssetAssignmentRepository(TrackingType trackingType)
        : IAssetAssignmentRepository
    {
        public AssetAssignment? SavedAssignment { get; private set; }
        public AssetCondition? ReturnedCondition { get; private set; }

        public Employee Employee { get; init; } = new()
        {
            EmployeeCode = "E-1",
            EmployeeName = "Test Employee",
            Email = "employee@example.test",
            Department = "IT",
            IsActive = true,
        };

        public Task<AssetAssignmentListResult> GetPageAsync(
            AssetAssignmentListFilter filter,
            CancellationToken cancellationToken) =>
            Task.FromResult(new AssetAssignmentListResult([], 0));

        public Task<AssetAssignment?> GetByIdAsync(
            int assignmentId,
            CancellationToken cancellationToken) =>
            Task.FromResult<AssetAssignment?>(null);

        public Task<IReadOnlyList<AssetAssignment>> GetByEmployeeAsync(
            int employeeId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AssetAssignment>>([]);

        public Task<IReadOnlyList<AssetProduct>> GetActiveProductsAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AssetProduct>>([]);

        public Task<Employee?> GetEmployeeAsync(
            int employeeId,
            CancellationToken cancellationToken) =>
            Task.FromResult<Employee?>(Employee);

        public Task<AssetProduct?> GetProductAsync(
            int productId,
            CancellationToken cancellationToken) =>
            Task.FromResult<AssetProduct?>(new AssetProduct
            {
                ProductId = 5,
                CategoryId = 3,
                Category = new AssetCategory
                {
                    CategoryId = 3,
                    CategoryName = "Computers",
                    IsActive = true,
                },
                ProductName = "Laptop",
                Brand = "Example",
                TrackingType = trackingType,
                IsActive = true,
            });

        public Task<IReadOnlyList<AssetUnit>> GetAvailableUnitsAsync(
            int productId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AssetUnit>>([]);

        public Task<IReadOnlyList<AssetAssignmentBatchData>> GetAvailableBatchesAsync(
            int productId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AssetAssignmentBatchData>>([]);

        public Task<(AssetAssignment Assignment, bool Created)> CreateAsync(
            AssetAssignment assignment,
            bool individual,
            CancellationToken cancellationToken)
        {
            assignment.AssignmentId = 42;
            assignment.Employee = Employee;
            var itemId = 1;
            foreach (var item in assignment.Items)
            {
                item.AssignmentItemId = itemId++;
                item.Product = new AssetProduct
                {
                    ProductId = item.ProductId,
                    ProductName = "Laptop",
                    Brand = "Example",
                    TrackingType = trackingType,
                    Category = new AssetCategory
                    {
                        CategoryName = "Computers",
                        IsActive = true,
                    },
                };
            }

            SavedAssignment = assignment;
            return Task.FromResult((assignment, true));
        }

        public Task<AssetAssignment> ReturnAsync(
            int assignmentId,
            ReturnAssetAssignmentRequest request,
            AssetCondition returnedCondition,
            string? actor,
            CancellationToken cancellationToken)
        {
            var assignment = SavedAssignment
                ?? throw new InvalidOperationException("Create an assignment before returning assets.");
            var item = assignment.Items.Single(item =>
                item.AssignmentItemId == request.AssignmentItemId);
            item.Returns.Add(new AssetReturn
            {
                ReturnId = 1,
                AssignmentItemId = item.AssignmentItemId,
                ReturnQuantity = request.Quantity,
                ReturnDate = request.ReturnDate,
                ReturnedCondition = returnedCondition,
                Remarks = request.Remarks,
            });
            ReturnedCondition = returnedCondition;
            return Task.FromResult(assignment);
        }
    }
}
