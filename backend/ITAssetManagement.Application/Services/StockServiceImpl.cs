using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Domain.Entities;

namespace ITAssetManagement.Application.Services;

public sealed class StockServiceImpl(IStockRepository repository) : IStockService
{
    public async Task<IReadOnlyList<StockProductDto>> GetProductsAsync(
        CancellationToken cancellationToken)
    {
        var products = await repository.GetActiveProductsAsync(cancellationToken);
        return products.Select(ToStockProduct).ToArray();
    }

    public async Task<StockReceiptSaveResult> ReceiveIndividualAsync(
        ReceiveIndividualStockRequest request,
        string? actor,
        CancellationToken cancellationToken)
    {
        ValidateIdempotencyKey(request.IdempotencyKey);
        if (request.Assets is null || request.Assets.Count is < 1 or > 200)
        {
            throw new InvalidStockRequestException("Receive between 1 and 200 individual assets per receipt.");
        }

        var normalizedAssets = new List<NormalizedAsset>(request.Assets.Count);
        var serialNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in request.Assets)
        {
            var serialNumber = NormalizeSerialNumber(asset.SerialNumber);
            if (serialNumber is not null && !serialNumbers.Add(serialNumber))
            {
                throw new DuplicateStockIdentifierException(
                    "Serial numbers must be unique within the receipt.");
            }

            var purchaseDate = NormalizeDate(asset.PurchaseDate, "Purchase date");
            var warrantyExpiryDate = NormalizeOptionalDate(asset.WarrantyExpiryDate);
            ValidateWarranty(purchaseDate, warrantyExpiryDate);
            if (asset.PurchaseCost < 0)
            {
                throw new InvalidStockRequestException("Purchase cost cannot be negative.");
            }

            var condition = ParseCondition(asset.Condition);
            normalizedAssets.Add(new NormalizedAsset(
                serialNumber,
                purchaseDate,
                warrantyExpiryDate,
                asset.PurchaseCost,
                condition));
        }

        var receiptNumber = ReceiptNumber(request.IdempotencyKey);
        var existing = await repository.GetReceiptByNumberAsync(receiptNumber, cancellationToken);
        if (existing is not null)
        {
            if (!MatchesIndividualReceipt(existing, request, normalizedAssets))
            {
                throw new StockIdempotencyConflictException(
                    "This idempotency key was already used for a different receipt.");
            }

            return new StockReceiptSaveResult(ToResponse(existing), false);
        }

        var product = await GetEligibleProductAsync(
            request.ProductId,
            TrackingType.Individual,
            cancellationToken);
        await EnsureSerialNumbersAvailableAsync(normalizedAssets, cancellationToken);
        var receipt = CreateReceipt(
            request.IdempotencyKey,
            NormalizeOptional(request.Supplier),
            NormalizeOptional(request.InvoiceNumber),
            NormalizeOptional(request.Remarks),
            actor);

        foreach (var asset in normalizedAssets)
        {
            var item = CreateReceiptItem(
                receipt,
                product,
                quantity: 1,
                asset.PurchaseCost,
                asset.PurchaseDate,
                asset.WarrantyExpiryDate,
                asset.Condition,
                actor);
            var unit = new AssetUnit
            {
                ProductId = product.ProductId,
                Product = product,
                AssetCode = $"PENDING-{Guid.NewGuid():N}",
                SerialNumber = asset.SerialNumber,
                PurchaseDate = asset.PurchaseDate,
                WarrantyExpiryDate = asset.WarrantyExpiryDate,
                PurchaseCost = asset.PurchaseCost,
                Status = asset.Condition == AssetCondition.Damaged
                    ? AssetStatus.UnderRepair
                    : AssetStatus.Available,
                Condition = asset.Condition,
                CreatedBy = NormalizeActor(actor),
            };
            item.StockMovements.Add(CreateReceiptMovement(
                product.ProductId, item, unit, 1, receipt, actor));
        }

        var saved = await repository.SaveReceiptAsync(receipt, cancellationToken);
        if (!saved.Created && !MatchesIndividualReceipt(saved.Receipt, request, normalizedAssets))
        {
            throw new StockIdempotencyConflictException(
                "This idempotency key was already used for a different receipt.");
        }

        return new StockReceiptSaveResult(ToResponse(saved.Receipt), saved.Created);
    }

    public async Task<StockReceiptSaveResult> ReceiveBulkAsync(
        ReceiveBulkStockRequest request,
        string? actor,
        CancellationToken cancellationToken)
    {
        ValidateIdempotencyKey(request.IdempotencyKey);
        if (request.Quantity <= 0)
        {
            throw new InvalidStockRequestException("Received quantity must be greater than zero.");
        }

        if (request.UnitPrice < 0)
        {
            throw new InvalidStockRequestException("Unit price cannot be negative.");
        }

        var purchaseDate = NormalizeDate(request.PurchaseDate, "Purchase date");
        var warrantyExpiryDate = NormalizeOptionalDate(request.WarrantyExpiryDate);
        ValidateWarranty(purchaseDate, warrantyExpiryDate);
        var condition = ParseCondition(request.Condition);
        var supplier = NormalizeOptional(request.Supplier);
        var invoiceNumber = NormalizeOptional(request.InvoiceNumber);
        var remarks = NormalizeOptional(request.Remarks);
        var existing = await repository.GetReceiptByNumberAsync(
            ReceiptNumber(request.IdempotencyKey), cancellationToken);
        if (existing is not null)
        {
            if (!MatchesBulkReceipt(
                    existing,
                    request.ProductId,
                    request.Quantity,
                    request.UnitPrice,
                    purchaseDate,
                    warrantyExpiryDate,
                    condition,
                    supplier,
                    invoiceNumber,
                    remarks))
            {
                throw new StockIdempotencyConflictException(
                    "This idempotency key was already used for a different receipt.");
            }

            return new StockReceiptSaveResult(ToResponse(existing), false);
        }

        var product = await GetEligibleProductAsync(
            request.ProductId,
            TrackingType.Bulk,
            cancellationToken);
        var receipt = CreateReceipt(request.IdempotencyKey, supplier, invoiceNumber, remarks, actor);
        var item = CreateReceiptItem(
            receipt,
            product,
            request.Quantity,
            request.UnitPrice,
            purchaseDate,
            warrantyExpiryDate,
            condition,
            actor);
        item.StockMovements.Add(CreateReceiptMovement(
            product.ProductId, item, assetUnit: null, request.Quantity, receipt, actor));

        var saved = await repository.SaveReceiptAsync(receipt, cancellationToken);
        if (!saved.Created
            && !MatchesBulkReceipt(
                saved.Receipt,
                request.ProductId,
                request.Quantity,
                request.UnitPrice,
                purchaseDate,
                warrantyExpiryDate,
                condition,
                supplier,
                invoiceNumber,
                remarks))
        {
            throw new StockIdempotencyConflictException(
                "This idempotency key was already used for a different receipt.");
        }

        return new StockReceiptSaveResult(ToResponse(saved.Receipt), saved.Created);
    }

    public async Task<PagedStockReceiptsResponse> GetReceiptsAsync(
        string? search,
        int? productId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken)
    {
        ValidatePagination(page, pageSize);
        if (productId is <= 0)
        {
            throw new InvalidStockRequestException("Product ID must be positive.");
        }

        DateTime? start = startDate.HasValue
            ? NormalizeDate(startDate.Value, "Start date")
            : null;
        DateTime? end = endDate.HasValue
            ? NormalizeDate(endDate.Value, "End date")
            : null;
        if (start.HasValue && end.HasValue && end.Value < start.Value)
        {
            throw new InvalidStockRequestException("End date cannot be before start date.");
        }

        var normalizedSortBy = NormalizeSortBy(sortBy);
        var normalizedSortDirection = string.Equals(
            sortDirection?.Trim(), "desc", StringComparison.OrdinalIgnoreCase)
            ? "desc"
            : "asc";
        var (receipts, totalCount) = await repository.GetReceiptsAsync(
            new StockReceiptFilter(
                NormalizeOptional(search),
                productId,
                start,
                end,
                page,
                pageSize,
                normalizedSortBy,
                normalizedSortDirection),
            cancellationToken);
        return new PagedStockReceiptsResponse(
            receipts.Select(ToSummary).ToArray(), totalCount, page, pageSize);
    }

    public async Task<StockReceiptResponse?> GetReceiptAsync(
        int receiptId,
        CancellationToken cancellationToken)
    {
        if (receiptId <= 0)
        {
            throw new InvalidStockRequestException("Receipt ID must be positive.");
        }

        var receipt = await repository.GetReceiptAsync(receiptId, cancellationToken);
        return receipt is null ? null : ToResponse(receipt);
    }

    public async Task<IReadOnlyList<StockAvailabilityResponse>> GetAvailabilityAsync(
        CancellationToken cancellationToken)
    {
        var products = await repository.GetProductsForAvailabilityAsync(cancellationToken);
        var balances = (await repository.GetBulkBalancesAsync(cancellationToken))
            .Concat(await repository.GetUnitBalancesAsync(cancellationToken))
            .ToDictionary(balance => balance.ProductId);
        return products
            .Select(product => ToAvailability(
                product,
                balances.GetValueOrDefault(product.ProductId)))
            .ToArray();
    }

    public async Task<StockAvailabilityResponse?> GetAvailabilityAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            throw new InvalidStockRequestException("Product ID must be positive.");
        }

        var products = await repository.GetProductsForAvailabilityAsync(cancellationToken);
        var product = products.SingleOrDefault(item => item.ProductId == productId);
        if (product is null)
        {
            return null;
        }

        var balance = product.TrackingType == TrackingType.Bulk
            ? await repository.GetBulkBalanceAsync(productId, cancellationToken)
            : (await repository.GetUnitBalancesAsync(cancellationToken))
                .SingleOrDefault(item => item.ProductId == productId);
        return ToAvailability(product, balance);
    }

    public async Task<IReadOnlyList<StockAssetUnitResponse>?> GetProductUnitsAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            throw new InvalidStockRequestException("Product ID must be positive.");
        }

        var units = await repository.GetProductUnitsAsync(productId, cancellationToken);
        if (units.Count == 0
            && !await repository.ProductExistsAsync(productId, cancellationToken))
        {
            return null;
        }

        return units.Select(ToUnitResponse).ToArray();
    }

    public async Task<AssetBarcodeDetailsResponse?> GetAssetByCodeAsync(
        string assetCode,
        CancellationToken cancellationToken)
    {
        var normalizedCode = assetCode.Trim();
        if (normalizedCode.Length is 0 or > 100)
        {
            throw new InvalidStockRequestException("Enter a valid asset barcode.");
        }

        var unit = await repository.GetAssetUnitByCodeAsync(normalizedCode, cancellationToken);
        if (unit is null)
        {
            return null;
        }

        var currentAssignment = unit.AssignmentItems
            .Where(item => item.Assignment is not null
                && item.Quantity > item.Returns.Sum(assetReturn => assetReturn.ReturnQuantity))
            .OrderByDescending(item => item.Assignment!.AssignedDate)
            .ThenByDescending(item => item.Assignment!.AssignmentId)
            .FirstOrDefault()?.Assignment;
        return new AssetBarcodeDetailsResponse(
            unit.AssetUnitId,
            unit.ProductId,
            unit.AssetCode,
            unit.Product?.ProductName ?? string.Empty,
            unit.Product?.TrackingType.ToString() ?? string.Empty,
            unit.Product?.Category?.CategoryName ?? string.Empty,
            unit.Product?.Brand ?? string.Empty,
            unit.Product?.Model,
            unit.SerialNumber,
            currentAssignment?.Employee?.EmployeeName,
            currentAssignment?.Employee?.Department,
            currentAssignment?.Employee?.Location,
            unit.Status.ToString(),
            unit.PurchaseDate);
    }

    public async Task<BulkStockConversionResponse> ConvertBulkStockToIndividualAsync(
        int productId,
        string? actor,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            throw new InvalidStockRequestException("Product ID must be positive.");
        }

        var assetsCreated = await repository.ConvertBulkStockToIndividualAsync(
            productId, actor, cancellationToken);
        return new BulkStockConversionResponse(productId, assetsCreated);
    }

    public async Task<PagedStockMovementsResponse> GetMovementsAsync(
        int? productId,
        string? movementType,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePagination(page, pageSize);
        if (productId is <= 0)
        {
            throw new InvalidStockRequestException("Product ID must be positive.");
        }

        if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
        {
            throw new InvalidStockRequestException("Start date cannot be after end date.");
        }

        string? normalizedMovementType = null;
        if (!string.IsNullOrWhiteSpace(movementType))
        {
            if (!Enum.TryParse<MovementType>(movementType.Trim(), true, out var parsedMovementType)
                || !Enum.IsDefined(parsedMovementType))
            {
                throw new InvalidStockRequestException("Select a valid stock movement type.");
            }

            normalizedMovementType = parsedMovementType.ToString();
        }

        var (movements, totalCount) = await repository.GetMovementsAsync(
            new StockMovementFilter(
                productId,
                normalizedMovementType,
                startDate?.Date,
                endDate?.Date,
                page,
                pageSize),
            cancellationToken);
        var items = movements.Select(movement => new StockMovementResponse(
            movement.MovementId,
            movement.ProductId,
            movement.Product?.ProductName ?? string.Empty,
            movement.ReceiptItemId,
            movement.AssetUnitId,
            movement.AssetUnit?.AssetCode,
            movement.MovementType.ToString(),
            movement.Quantity,
            movement.MovementDate,
            movement.ReferenceId,
            movement.Remarks)).ToArray();
        return new PagedStockMovementsResponse(items, totalCount, page, pageSize);
    }

    private async Task<AssetProduct> GetEligibleProductAsync(
        int productId,
        TrackingType expectedTrackingType,
        CancellationToken cancellationToken)
    {
        if (productId <= 0)
        {
            throw new InvalidStockRequestException("Select an active product.");
        }

        var product = await repository.GetProductAsync(productId, cancellationToken);
        if (product is not { IsActive: true })
        {
            throw new InvalidStockRequestException("Select an existing active product.");
        }

        if (product.TrackingType != expectedTrackingType)
        {
            throw new InvalidStockRequestException(
                $"This product uses {product.TrackingType} tracking and cannot be received as {expectedTrackingType} stock.");
        }

        return product;
    }

    private async Task EnsureSerialNumbersAvailableAsync(
        IEnumerable<NormalizedAsset> assets,
        CancellationToken cancellationToken)
    {
        foreach (var serialNumber in assets
                     .Select(asset => asset.SerialNumber)
                     .Where(serialNumber => serialNumber is not null)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (await repository.SerialNumberExistsAsync(serialNumber!, cancellationToken))
            {
                throw new DuplicateStockIdentifierException(
                    $"Serial number '{serialNumber}' is already registered.");
            }
        }
    }

    private static StockReceipt CreateReceipt(
        Guid idempotencyKey,
        string? supplier,
        string? invoiceNumber,
        string? remarks,
        string? actor)
    {
        return new StockReceipt
        {
            ReceiptNumber = ReceiptNumber(idempotencyKey),
            ReceivedDate = DateTime.UtcNow,
            Supplier = supplier,
            InvoiceNumber = invoiceNumber,
            Remarks = remarks,
            CreatedBy = NormalizeActor(actor),
        };
    }

    private static StockReceiptItem CreateReceiptItem(
        StockReceipt receipt,
        AssetProduct product,
        int quantity,
        decimal unitPrice,
        DateTime purchaseDate,
        DateTime? warrantyExpiryDate,
        AssetCondition condition,
        string? actor)
    {
        var item = new StockReceiptItem
        {
            Receipt = receipt,
            Product = product,
            ProductId = product.ProductId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            PurchaseDate = purchaseDate,
            WarrantyExpiryDate = warrantyExpiryDate,
            Condition = condition,
            CreatedBy = NormalizeActor(actor),
        };
        receipt.Items.Add(item);
        return item;
    }

    private static StockMovement CreateReceiptMovement(
        int productId,
        StockReceiptItem item,
        AssetUnit? assetUnit,
        int quantity,
        StockReceipt receipt,
        string? actor)
    {
        return new StockMovement
        {
            ProductId = productId,
            ReceiptItem = item,
            ReceiptItemId = item.ReceiptItemId,
            AssetUnit = assetUnit,
            AssetUnitId = assetUnit?.AssetUnitId,
            MovementType = MovementType.Receipt,
            Quantity = quantity,
            MovementDate = DateTime.UtcNow,
            Remarks = receipt.Remarks,
            CreatedBy = NormalizeActor(actor),
        };
    }

    private static string ReceiptNumber(Guid idempotencyKey)
    {
        return $"REC-{idempotencyKey:N}";
    }

    private static StockProductDto ToStockProduct(AssetProduct product)
    {
        return new StockProductDto(
            product.ProductId,
            product.CategoryId,
            product.Category?.CategoryName ?? string.Empty,
            product.ProductName,
            product.Brand ?? string.Empty,
            product.Model,
            product.TrackingType.ToString());
    }

    private static StockReceiptSummaryResponse ToSummary(StockReceipt receipt)
    {
        var products = receipt.Items
            .Select(item => item.Product?.ProductName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var trackingTypes = receipt.Items
            .Select(item => item.Product?.TrackingType.ToString())
            .Where(type => type is not null)
            .Distinct()
            .ToArray();
        return new StockReceiptSummaryResponse(
            receipt.ReceiptId,
            receipt.ReceiptNumber,
            receipt.ReceivedDate,
            products.Length == 1 ? products[0]! : string.Join(", ", products),
            trackingTypes.Length == 1 ? trackingTypes[0]! : "Mixed",
            receipt.Items.Sum(item => item.Quantity),
            receipt.Supplier,
            receipt.InvoiceNumber,
            receipt.Items.Sum(item => item.Quantity * item.UnitPrice),
            receipt.CreatedBy);
    }

    private static StockReceiptResponse ToResponse(StockReceipt receipt)
    {
        var items = receipt.Items.Select(item => new StockReceiptItemResponse(
            item.ReceiptItemId,
            item.ProductId,
            item.Product?.ProductName ?? string.Empty,
            item.Product?.Brand ?? string.Empty,
            item.Product?.Model,
            item.Quantity,
            item.UnitPrice,
            item.PurchaseDate,
            item.WarrantyExpiryDate,
            item.Condition.ToString(),
            item.StockMovements
                .Where(movement => movement.AssetUnit is not null)
                .Select(movement => ToUnitResponse(movement.AssetUnit!))
                .ToArray())).ToArray();
        var trackingTypes = receipt.Items
            .Select(item => item.Product?.TrackingType.ToString())
            .Where(type => type is not null)
            .Distinct()
            .ToArray();

        return new StockReceiptResponse(
            receipt.ReceiptId,
            receipt.ReceiptNumber,
            receipt.ReceivedDate,
            receipt.Supplier,
            receipt.InvoiceNumber,
            receipt.Remarks,
            receipt.CreatedBy,
            trackingTypes.Length == 1 ? trackingTypes[0]! : "Mixed",
            items.Sum(item => item.Quantity),
            items.Sum(item => item.Quantity * item.UnitPrice),
            items);
    }

    private static StockAssetUnitResponse ToUnitResponse(AssetUnit unit)
    {
        return new StockAssetUnitResponse(
            unit.AssetUnitId,
            unit.AssetCode,
            unit.SerialNumber,
            unit.PurchaseDate,
            unit.WarrantyExpiryDate,
            unit.PurchaseCost,
            unit.Status.ToString(),
            unit.Condition.ToString());
    }

    private static StockAvailabilityResponse ToAvailability(
        AssetProduct product,
        StockBalanceData? bulkBalance)
    {
        return new StockAvailabilityResponse(
            product.ProductId,
            product.ProductName,
            product.Category?.CategoryName ?? string.Empty,
            product.TrackingType.ToString(),
            bulkBalance?.AvailableQuantity ?? 0,
            bulkBalance?.AssignedQuantity ?? 0,
            bulkBalance?.UnavailableQuantity ?? 0,
            bulkBalance?.DamagedQuantity ?? 0,
            product.MinimumStock);
    }

    private static string? NormalizeSerialNumber(string? serialNumber)
    {
        var normalized = serialNumber?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized.ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static string? NormalizeActor(string? actor)
    {
        var normalized = actor?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized[..Math.Min(normalized.Length, 100)];
    }

    private static DateTime NormalizeDate(DateTime value, string fieldName)
    {
        if (value == default)
        {
            throw new InvalidStockRequestException($"{fieldName} is required.");
        }

        return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    }

    private static DateTime? NormalizeOptionalDate(DateTime? value)
    {
        return value.HasValue
            ? DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc)
            : null;
    }

    private static void ValidateWarranty(DateTime purchaseDate, DateTime? warrantyExpiryDate)
    {
        if (warrantyExpiryDate < purchaseDate)
        {
            throw new InvalidStockRequestException(
                "Warranty expiry date cannot be earlier than the purchase date.");
        }
    }

    private static AssetCondition ParseCondition(string? value)
    {
        if (Enum.TryParse<AssetCondition>(value?.Trim(), ignoreCase: true, out var condition)
            && condition is AssetCondition.New or AssetCondition.Good or AssetCondition.Damaged)
        {
            return condition;
        }

        throw new InvalidStockRequestException("Condition must be New, Good, or Damaged.");
    }

    private static void ValidateIdempotencyKey(Guid key)
    {
        if (key == Guid.Empty)
        {
            throw new InvalidStockRequestException("A non-empty idempotency key is required.");
        }
    }

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new InvalidStockRequestException(
                "Page must be positive and page size must be between 1 and 100.");
        }
    }

    private static string NormalizeSortBy(string? sortBy)
    {
        return sortBy?.Trim().ToLowerInvariant() switch
        {
            "receiptnumber" => "receiptnumber",
            "productname" => "productname",
            "trackingtype" => "trackingtype",
            "quantity" => "quantity",
            "supplier" => "supplier",
            "invoicenumber" => "invoicenumber",
            "totalamount" => "totalamount",
            "receivedby" => "receivedby",
            _ => "receiveddate",
        };
    }

    private static bool MatchesIndividualReceipt(
        StockReceipt receipt,
        ReceiveIndividualStockRequest request,
        IReadOnlyList<NormalizedAsset> normalizedAssets)
    {
        if (!string.Equals(receipt.Supplier, NormalizeOptional(request.Supplier), StringComparison.Ordinal)
            || !string.Equals(receipt.InvoiceNumber, NormalizeOptional(request.InvoiceNumber), StringComparison.Ordinal)
            || !string.Equals(receipt.Remarks, NormalizeOptional(request.Remarks), StringComparison.Ordinal)
            || receipt.Items.Count != normalizedAssets.Count
            || receipt.Items.Any(item => item.ProductId != request.ProductId || item.Quantity != 1))
        {
            return false;
        }

        var requested = normalizedAssets.Select(AssetKey).Order(StringComparer.Ordinal).ToArray();
        var stored = receipt.Items
            .Select(item => item.StockMovements.FirstOrDefault()?.AssetUnit)
            .Where(unit => unit is not null)
            .Select(unit => AssetKey(new NormalizedAsset(
                NormalizeSerialNumber(unit!.SerialNumber),
                unit.PurchaseDate,
                unit.WarrantyExpiryDate,
                unit.PurchaseCost,
                unit.Condition)))
            .Order(StringComparer.Ordinal)
            .ToArray();
        return requested.SequenceEqual(stored, StringComparer.Ordinal);
    }

    private static bool MatchesBulkReceipt(
        StockReceipt receipt,
        int productId,
        int quantity,
        decimal unitPrice,
        DateTime purchaseDate,
        DateTime? warrantyExpiryDate,
        AssetCondition condition,
        string? supplier,
        string? invoiceNumber,
        string? remarks)
    {
        if (!string.Equals(receipt.Supplier, supplier, StringComparison.Ordinal)
            || !string.Equals(receipt.InvoiceNumber, invoiceNumber, StringComparison.Ordinal)
            || !string.Equals(receipt.Remarks, remarks, StringComparison.Ordinal)
            || receipt.Items.Count != 1)
        {
            return false;
        }

        var item = receipt.Items.Single();
        return item.ProductId == productId
            && item.Quantity == quantity
            && item.UnitPrice == unitPrice
            && item.PurchaseDate == purchaseDate
            && item.WarrantyExpiryDate == warrantyExpiryDate
            && item.Condition == condition;
    }

    private static string AssetKey(NormalizedAsset asset)
    {
        return string.Join(
            "|",
            asset.SerialNumber ?? string.Empty,
            asset.PurchaseDate.Ticks,
            asset.WarrantyExpiryDate?.Ticks.ToString() ?? string.Empty,
            asset.PurchaseCost,
            (int)asset.Condition);
    }

    private sealed record NormalizedAsset(
        string? SerialNumber,
        DateTime PurchaseDate,
        DateTime? WarrantyExpiryDate,
        decimal PurchaseCost,
        AssetCondition Condition);
}
