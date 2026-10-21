using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Application.Services;
using ITAssetManagement.Domain.Entities;

namespace ITAssetManagement.Tests;

public sealed class StockServiceTests
{
    [Fact]
    public async Task ReceiveIndividualAsync_CreatesOneAssetAndSetsAuditing()
    {
        var (service, repository, product) = CreateService(TrackingType.Individual);

        var result = await service.ReceiveIndividualAsync(
            IndividualRequest(product.ProductId, [Asset(" serial-1 ")]),
            "it-staff",
            CancellationToken.None);

        var item = Assert.Single(repository.Receipts.Single().Items);
        var movement = Assert.Single(item.StockMovements);
        Assert.True(result.Created);
        Assert.Equal("it-staff", result.Receipt.ReceivedBy);
        Assert.Equal(1, result.Receipt.Quantity);
        Assert.Equal(1, item.Quantity);
        Assert.Equal("SERIAL-1", item.StockMovements.Single().AssetUnit?.SerialNumber);
        Assert.StartsWith("AST-", movement.AssetUnit?.AssetCode);
        Assert.Equal(AssetStatus.Available, movement.AssetUnit?.Status);
        Assert.Equal(MovementType.Receipt, movement.MovementType);
        Assert.Equal(item.ReceiptId, movement.ReferenceId);
        Assert.Equal("it-staff", movement.CreatedBy);
    }

    [Fact]
    public async Task ReceiveIndividualAsync_CreatesEachRowAndMarksDamagedUnavailable()
    {
        var (service, repository, product) = CreateService(TrackingType.Individual);

        var result = await service.ReceiveIndividualAsync(
            IndividualRequest(product.ProductId, [Asset("S-1"), Asset("S-2", condition: "Damaged")]),
            "it-staff",
            CancellationToken.None);

        var receipt = Assert.Single(repository.Receipts);
        Assert.Equal(2, result.Receipt.Quantity);
        Assert.Equal(2, receipt.Items.Count);
        Assert.All(receipt.Items, item => Assert.Equal(1, item.Quantity));
        Assert.Equal(2, receipt.Items.SelectMany(item => item.StockMovements).Count());
        Assert.Equal(
            AssetStatus.UnderRepair,
            receipt.Items.SelectMany(item => item.StockMovements)
                .Single(movement => movement.AssetUnit?.Condition == AssetCondition.Damaged)
                .AssetUnit?.Status);
    }

    [Fact]
    public async Task ReceiveBulkAsync_CreatesQuantityLineWithoutAssetUnits()
    {
        var (service, repository, product) = CreateService(TrackingType.Bulk);
        var request = BulkRequest(product.ProductId, quantity: 25);

        var result = await service.ReceiveBulkAsync(request, "it-staff", CancellationToken.None);

        var receipt = Assert.Single(repository.Receipts);
        var item = Assert.Single(receipt.Items);
        var movement = Assert.Single(item.StockMovements);
        Assert.True(result.Created);
        Assert.Equal(25, result.Receipt.Quantity);
        Assert.Equal(25, item.Quantity);
        Assert.Equal(25, movement.Quantity);
        Assert.Null(movement.AssetUnit);
        Assert.Equal(25, result.Receipt.TotalAmount);
        Assert.Equal(AssetCondition.Good, item.Condition);
    }

    [Fact]
    public async Task ReceiveBulkAsync_ReplayingSameKeyDoesNotDuplicateReceiptOrMovement()
    {
        var (service, repository, product) = CreateService(TrackingType.Bulk);
        var request = BulkRequest(product.ProductId, quantity: 10);

        var first = await service.ReceiveBulkAsync(request, "it-staff", CancellationToken.None);
        var replay = await service.ReceiveBulkAsync(request, "it-staff", CancellationToken.None);

        Assert.True(first.Created);
        Assert.False(replay.Created);
        Assert.Equal(first.Receipt.ReceiptId, replay.Receipt.ReceiptId);
        Assert.Single(repository.Receipts);
        Assert.Single(repository.Receipts.Single().Items.Single().StockMovements);
    }

    [Fact]
    public async Task ReceiveBulkAsync_RejectsSameIdempotencyKeyWithChangedRequest()
    {
        var (service, repository, product) = CreateService(TrackingType.Bulk);
        var request = BulkRequest(product.ProductId, quantity: 10);
        await service.ReceiveBulkAsync(request, "it-staff", CancellationToken.None);

        await Assert.ThrowsAsync<StockIdempotencyConflictException>(() =>
            service.ReceiveBulkAsync(
                request with { Quantity = 11 }, "it-staff", CancellationToken.None));
        Assert.Single(repository.Receipts);
    }

    [Fact]
    public async Task ReceiveIndividualAsync_RejectsRepeatedSerialNumbers()
    {
        var (service, repository, product) = CreateService(TrackingType.Individual);

        await Assert.ThrowsAsync<DuplicateStockIdentifierException>(() =>
            service.ReceiveIndividualAsync(
                IndividualRequest(product.ProductId, [Asset("s-1"), Asset(" S-1 ")]),
                "it-staff",
                CancellationToken.None));
        Assert.Empty(repository.Receipts);
    }

    [Fact]
    public async Task ReceiveIndividualAsync_RejectsExistingSerialNumbersIgnoringCase()
    {
        var (service, repository, product) = CreateService(TrackingType.Individual);
        repository.ExistingSerialNumbers.Add("serial-1");

        await Assert.ThrowsAsync<DuplicateStockIdentifierException>(() =>
            service.ReceiveIndividualAsync(
                IndividualRequest(product.ProductId, [Asset(" SERIAL-1 ")]),
                "it-staff",
                CancellationToken.None));
        Assert.Empty(repository.Receipts);
    }

    [Fact]
    public async Task GetMovementsAsync_PassesValidatedFiltersToRepository()
    {
        var (service, repository, product) = CreateService(TrackingType.Bulk);

        var response = await service.GetMovementsAsync(
            product.ProductId,
            " return ",
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 31),
            2,
            25,
            CancellationToken.None);

        Assert.Empty(response.Items);
        Assert.Equal(new StockMovementFilter(
            product.ProductId,
            "Return",
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 31),
            2,
            25), repository.LastMovementFilter);
    }

    [Fact]
    public async Task GetMovementsAsync_RejectsInvalidMovementTypeAndDateRange()
    {
        var (service, repository, product) = CreateService(TrackingType.Bulk);

        await Assert.ThrowsAsync<InvalidStockRequestException>(() =>
            service.GetMovementsAsync(
                product.ProductId, "99", null, null, 1, 25, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidStockRequestException>(() =>
            service.GetMovementsAsync(
                product.ProductId,
                null,
                new DateTime(2026, 10, 31),
                new DateTime(2026, 10, 1),
                1,
                25,
                CancellationToken.None));

        Assert.Null(repository.LastMovementFilter);
    }

    [Fact]
    public async Task GetAssetByCodeAsync_ReturnsAssetAndCurrentAssignmentDetails()
    {
        var (service, repository, product) = CreateService(TrackingType.Individual);
        repository.BarcodeAsset = new AssetUnit
        {
            AssetUnitId = 17,
            ProductId = product.ProductId,
            AssetCode = "AST-000017",
            SerialNumber = "SERIAL-17",
            PurchaseDate = new DateTime(2026, 1, 10),
            Status = AssetStatus.Assigned,
            Condition = AssetCondition.Good,
            Product = product,
            AssignmentItems =
            [
                new AssetAssignmentItem
                {
                    Quantity = 1,
                    Assignment = new AssetAssignment
                    {
                        AssignmentId = 3,
                        AssignedDate = new DateTime(2026, 2, 1),
                        AssignedBy = "it-staff",
                        Employee = new Employee
                        {
                            EmployeeCode = "E-17",
                            EmployeeName = "Test Employee",
                            Email = "employee@example.test",
                            Department = "IT",
                            Location = "Building A",
                        },
                    },
                },
            ],
        };

        var result = await service.GetAssetByCodeAsync(
            " AST-000017 ", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(17, result.AssetUnitId);
        Assert.Equal(product.ProductId, result.ProductId);
        Assert.Equal("AST-000017", result.AssetCode);
        Assert.Equal("Laptop", result.AssetName);
        Assert.Equal("Hardware", result.CategoryName);
        Assert.Equal("Acme", result.Manufacturer);
        Assert.Equal("Test Employee", result.AssignedEmployee);
        Assert.Equal("IT", result.Department);
        Assert.Equal("Building A", result.CurrentLocation);
        Assert.Equal("Assigned", result.Status);
        Assert.Equal("AST-000017", repository.LastAssetCodeLookup);
    }

    [Fact]
    public async Task GetAssetByCodeAsync_ReturnsNullWhenAssetDoesNotExist()
    {
        var (service, repository, _) = CreateService(TrackingType.Individual);

        var result = await service.GetAssetByCodeAsync("AST-404", CancellationToken.None);

        Assert.Null(result);
        Assert.Equal("AST-404", repository.LastAssetCodeLookup);
    }

    [Fact]
    public async Task ConvertBulkStockToIndividualAsyncReturnsCreatedAssetCount()
    {
        var (service, repository, product) = CreateService(TrackingType.Bulk);
        repository.ConvertedAssetCount = 10;

        var result = await service.ConvertBulkStockToIndividualAsync(
            product.ProductId, "it-staff", CancellationToken.None);

        Assert.Equal(product.ProductId, result.ProductId);
        Assert.Equal(10, result.AssetsCreated);
        Assert.Equal(product.ProductId, repository.LastConvertedProductId);
        Assert.Equal("it-staff", repository.LastConversionActor);
    }

    [Fact]
    public async Task ConvertBulkStockToIndividualAsyncRejectsInvalidProductId()
    {
        var (service, repository, _) = CreateService(TrackingType.Bulk);

        await Assert.ThrowsAsync<InvalidStockRequestException>(() =>
            service.ConvertBulkStockToIndividualAsync(0, "it-staff", CancellationToken.None));

        Assert.Null(repository.LastConvertedProductId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901")]
    public async Task GetAssetByCodeAsync_RejectsInvalidBarcode(string assetCode)
    {
        var (service, repository, _) = CreateService(TrackingType.Individual);

        await Assert.ThrowsAsync<InvalidStockRequestException>(() =>
            service.GetAssetByCodeAsync(assetCode, CancellationToken.None));

        Assert.Null(repository.LastAssetCodeLookup);
    }

    [Theory]
    [InlineData(TrackingType.Bulk, "individual")]
    [InlineData(TrackingType.Individual, "bulk")]
    public async Task ReceiveAsync_RejectsTrackingTypeDifferentFromProduct(
        TrackingType productType,
        string operation)
    {
        var (service, repository, product) = CreateService(productType);

        if (operation == "individual")
        {
            await Assert.ThrowsAsync<InvalidStockRequestException>(() =>
                service.ReceiveIndividualAsync(
                    IndividualRequest(product.ProductId, [Asset("S-1")]),
                    "it-staff",
                    CancellationToken.None));
        }
        else
        {
            await Assert.ThrowsAsync<InvalidStockRequestException>(() =>
                service.ReceiveBulkAsync(
                    BulkRequest(product.ProductId),
                    "it-staff",
                    CancellationToken.None));
        }

        Assert.Empty(repository.Receipts);
    }

    [Fact]
    public async Task ReceiveBulkAsync_RejectsNonpositiveQuantityAndNegativePrice()
    {
        var (service, repository, product) = CreateService(TrackingType.Bulk);

        await Assert.ThrowsAsync<InvalidStockRequestException>(() =>
            service.ReceiveBulkAsync(
                BulkRequest(product.ProductId, quantity: 0),
                "it-staff",
                CancellationToken.None));
        await Assert.ThrowsAsync<InvalidStockRequestException>(() =>
            service.ReceiveBulkAsync(
                BulkRequest(product.ProductId) with { UnitPrice = -1 },
                "it-staff",
                CancellationToken.None));
        Assert.Empty(repository.Receipts);
    }

    [Fact]
    public async Task ReceiveIndividualAsync_RejectsNegativePurchaseCost()
    {
        var (service, repository, product) = CreateService(TrackingType.Individual);

        await Assert.ThrowsAsync<InvalidStockRequestException>(() =>
            service.ReceiveIndividualAsync(
                IndividualRequest(product.ProductId, [Asset("S-1") with { PurchaseCost = -1 }]),
                "it-staff",
                CancellationToken.None));
        Assert.Empty(repository.Receipts);
    }

    [Fact]
    public async Task ReceiveBulkAsync_RejectsWarrantyEarlierThanPurchaseDate()
    {
        var (service, repository, product) = CreateService(TrackingType.Bulk);
        var request = BulkRequest(product.ProductId) with
        {
            PurchaseDate = new DateTime(2026, 10, 19),
            WarrantyExpiryDate = new DateTime(2026, 10, 18),
        };

        await Assert.ThrowsAsync<InvalidStockRequestException>(() =>
            service.ReceiveBulkAsync(request, "it-staff", CancellationToken.None));
        Assert.Empty(repository.Receipts);
    }

    [Fact]
    public async Task ReceiveBulkAsync_RejectsInactiveProduct()
    {
        var (service, repository, product) = CreateService(TrackingType.Bulk);
        product.IsActive = false;

        await Assert.ThrowsAsync<InvalidStockRequestException>(() =>
            service.ReceiveBulkAsync(
                BulkRequest(product.ProductId), "it-staff", CancellationToken.None));
        Assert.Empty(repository.Receipts);
    }

    private static (StockServiceImpl Service, FakeStockRepository Repository, AssetProduct Product)
        CreateService(TrackingType trackingType)
    {
        var category = new AssetCategory
        {
            CategoryId = 1,
            CategoryName = "Hardware",
            IsActive = true,
        };
        var product = new AssetProduct
        {
            ProductId = 10,
            CategoryId = category.CategoryId,
            Category = category,
            ProductName = "Laptop",
            Brand = "Acme",
            Model = "L1",
            TrackingType = trackingType,
            IsActive = true,
        };
        var repository = new FakeStockRepository(product);
        return (new StockServiceImpl(repository), repository, product);
    }

    private static ReceiveIndividualStockRequest IndividualRequest(
        int productId,
        IReadOnlyList<IndividualStockAssetRequest> assets)
    {
        return new ReceiveIndividualStockRequest(
            Guid.NewGuid(), productId, assets, "Supplier", "INV-1", "Receipt remarks");
    }

    private static IndividualStockAssetRequest Asset(
        string? serialNumber,
        string condition = "Good")
    {
        return new IndividualStockAssetRequest(
            serialNumber,
            new DateTime(2026, 10, 19),
            new DateTime(2027, 10, 19),
            65000m,
            condition);
    }

    private static ReceiveBulkStockRequest BulkRequest(
        int productId,
        int quantity = 25)
    {
        return new ReceiveBulkStockRequest(
            Guid.NewGuid(),
            productId,
            quantity,
            1m,
            new DateTime(2026, 10, 19),
            new DateTime(2027, 10, 19),
            "Good",
            "Supplier",
            "INV-1",
            "Receipt remarks");
    }

    private sealed class FakeStockRepository(AssetProduct product) : IStockRepository
    {
        public StockMovementFilter? LastMovementFilter { get; private set; }
        public AssetUnit? BarcodeAsset { get; set; }
        public string? LastAssetCodeLookup { get; private set; }
        public int ConvertedAssetCount { get; set; }
        public int? LastConvertedProductId { get; private set; }
        public string? LastConversionActor { get; private set; }

        private int nextReceiptId = 1;
        private int nextItemId = 1;
        private int nextUnitId = 1;

        public List<StockReceipt> Receipts { get; } = [];
        public HashSet<string> ExistingSerialNumbers { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<IReadOnlyList<AssetProduct>> GetActiveProductsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AssetProduct>>(
                product.IsActive ? [product] : []);
        }

        public Task<AssetProduct?> GetProductAsync(int productId, CancellationToken cancellationToken)
        {
            return Task.FromResult(product.ProductId == productId ? product : null);
        }

        public Task<bool> SerialNumberExistsAsync(
            string normalizedSerialNumber,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ExistingSerialNumbers.Contains(normalizedSerialNumber));
        }

        public Task<StockReceipt?> GetReceiptByNumberAsync(
            string receiptNumber,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Receipts.SingleOrDefault(
                receipt => receipt.ReceiptNumber == receiptNumber));
        }

        public Task<(StockReceipt Receipt, bool Created)> SaveReceiptAsync(
            StockReceipt receipt,
            CancellationToken cancellationToken)
        {
            var existing = Receipts.SingleOrDefault(
                item => item.ReceiptNumber == receipt.ReceiptNumber);
            if (existing is not null)
            {
                return Task.FromResult((existing, false));
            }

            receipt.ReceiptId = nextReceiptId++;
            foreach (var item in receipt.Items)
            {
                item.ReceiptId = receipt.ReceiptId;
                item.ReceiptItemId = nextItemId++;
                foreach (var movement in item.StockMovements)
                {
                    movement.ReceiptItemId = item.ReceiptItemId;
                    movement.ReferenceId = receipt.ReceiptId;
                    if (movement.AssetUnit is { } unit)
                    {
                        unit.AssetUnitId = nextUnitId++;
                        unit.AssetCode = $"AST-{unit.AssetUnitId:D6}";
                        movement.AssetUnitId = unit.AssetUnitId;
                    }
                }
            }

            Receipts.Add(receipt);
            return Task.FromResult((receipt, true));
        }

        public Task<(IReadOnlyList<StockReceipt> Receipts, int TotalCount)> GetReceiptsAsync(
            StockReceiptFilter filter,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(((IReadOnlyList<StockReceipt>)Receipts, Receipts.Count));
        }

        public Task<StockReceipt?> GetReceiptAsync(int receiptId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Receipts.SingleOrDefault(receipt => receipt.ReceiptId == receiptId));
        }

        public Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken)
        {
            return Task.FromResult(product.ProductId == productId);
        }

        public Task<IReadOnlyList<AssetProduct>> GetProductsForAvailabilityAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AssetProduct>>([product]);
        }

        public Task<IReadOnlyList<StockBalanceData>> GetBulkBalancesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<StockBalanceData>>([]);
        }

        public Task<IReadOnlyList<StockBalanceData>> GetUnitBalancesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<StockBalanceData>>([]);
        }

        public Task<StockBalanceData?> GetBulkBalanceAsync(int productId, CancellationToken cancellationToken)
        {
            return Task.FromResult<StockBalanceData?>(null);
        }

        public Task<IReadOnlyList<AssetUnit>> GetProductUnitsAsync(
            int productId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AssetUnit>>(
                Receipts.SelectMany(receipt => receipt.Items)
                    .SelectMany(item => item.StockMovements)
                    .Select(movement => movement.AssetUnit)
                    .Where(unit => unit is not null)
                    .Cast<AssetUnit>()
                    .ToArray());
        }

        public Task<AssetUnit?> GetAssetUnitByCodeAsync(
            string assetCode,
            CancellationToken cancellationToken)
        {
            LastAssetCodeLookup = assetCode;
            return Task.FromResult(
                BarcodeAsset?.AssetCode == assetCode ? BarcodeAsset : null);
        }

        public Task<int> ConvertBulkStockToIndividualAsync(
            int productId,
            string? actor,
            CancellationToken cancellationToken)
        {
            LastConvertedProductId = productId;
            LastConversionActor = actor;
            return Task.FromResult(ConvertedAssetCount);
        }

        public Task<(IReadOnlyList<StockMovement> Movements, int TotalCount)> GetMovementsAsync(
            StockMovementFilter filter,
            CancellationToken cancellationToken)
        {
            LastMovementFilter = filter;
            return Task.FromResult(((IReadOnlyList<StockMovement>)[], 0));
        }
    }
}
