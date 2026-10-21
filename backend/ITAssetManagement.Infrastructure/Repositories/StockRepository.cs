using System.Data;
using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Application.Services;
using ITAssetManagement.Domain.Entities;
using ITAssetManagement.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ITAssetManagement.Infrastructure.Repositories;

public sealed class StockRepository(AssetManagementDbContext dbContext) : IStockRepository
{
    public async Task<IReadOnlyList<AssetProduct>> GetActiveProductsAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.AssetProducts
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => product.IsActive && product.Category != null && product.Category.IsActive)
            .OrderBy(product => product.ProductName)
            .ToListAsync(cancellationToken);
    }

    public Task<AssetProduct?> GetProductAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        return dbContext.AssetProducts
            .AsNoTracking()
            .Include(product => product.Category)
            .SingleOrDefaultAsync(product => product.ProductId == productId, cancellationToken);
    }

    public Task<bool> SerialNumberExistsAsync(
        string normalizedSerialNumber,
        CancellationToken cancellationToken)
    {
        return dbContext.AssetUnits.AnyAsync(
            unit => unit.SerialNumber != null
                && unit.SerialNumber.Trim().ToUpper() == normalizedSerialNumber,
            cancellationToken);
    }

    public Task<StockReceipt?> GetReceiptByNumberAsync(
        string receiptNumber,
        CancellationToken cancellationToken)
    {
        return ReceiptQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(receipt => receipt.ReceiptNumber == receiptNumber, cancellationToken);
    }

    public async Task<(StockReceipt Receipt, bool Created)> SaveReceiptAsync(
        StockReceipt receipt,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction? transaction =
            dbContext.Database.CurrentTransaction is null
                ? await dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, cancellationToken)
                : null;
        var existing = await ReceiptQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.ReceiptNumber == receipt.ReceiptNumber,
                cancellationToken);
        if (existing is not null)
        {
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return (existing, false);
        }

        var productIds = receipt.Items.Select(item => item.ProductId).Distinct().ToArray();
        var products = await dbContext.AssetProducts
            .Include(product => product.Category)
            .Where(product => productIds.Contains(product.ProductId))
            .ToDictionaryAsync(product => product.ProductId, cancellationToken);
        foreach (var item in receipt.Items)
        {
            item.Product = products[item.ProductId];
            foreach (var movement in item.StockMovements)
            {
                movement.Product = products[item.ProductId];
                if (movement.AssetUnit is { } unit)
                {
                    unit.Product = products[item.ProductId];
                }
            }
        }

        try
        {
            dbContext.StockReceipts.Add(receipt);
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var item in receipt.Items)
            {
                foreach (var movement in item.StockMovements)
                {
                    movement.ReferenceId = receipt.ReceiptId;
                    if (movement.AssetUnit is { } unit)
                    {
                        unit.AssetCode = $"AST-{unit.AssetUnitId:D6}";
                    }
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return (receipt, true);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            dbContext.ChangeTracker.Clear();
            var duplicateRequest = await ReceiptQuery()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.ReceiptNumber == receipt.ReceiptNumber,
                    cancellationToken);
            if (duplicateRequest is not null)
            {
                return (duplicateRequest, false);
            }

            throw new DuplicateStockIdentifierException(
                "An asset code or serial number is already registered.");
        }
    }

    public async Task<(IReadOnlyList<StockReceipt> Receipts, int TotalCount)> GetReceiptsAsync(
        StockReceiptFilter filter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.StockReceipts
            .AsNoTracking()
            .Include(receipt => receipt.Items)
            .ThenInclude(item => item.Product)
            .AsQueryable();

        if (filter.ProductId.HasValue)
        {
            query = query.Where(receipt =>
                receipt.Items.Any(item => item.ProductId == filter.ProductId.Value));
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(receipt => receipt.ReceivedDate >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            var endExclusive = filter.EndDate.Value.AddDays(1);
            query = query.Where(receipt => receipt.ReceivedDate < endExclusive);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(receipt =>
                receipt.ReceiptNumber.Contains(search)
                || (receipt.Supplier != null && receipt.Supplier.Contains(search))
                || (receipt.InvoiceNumber != null && receipt.InvoiceNumber.Contains(search))
                || receipt.Items.Any(item =>
                    item.Product != null && item.Product.ProductName.Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var ordered = OrderReceipts(query, filter.SortBy, filter.SortDirection);
        var receipts = await ordered
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return (receipts, totalCount);
    }

    public Task<StockReceipt?> GetReceiptAsync(
        int receiptId,
        CancellationToken cancellationToken)
    {
        return ReceiptQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(receipt => receipt.ReceiptId == receiptId, cancellationToken);
    }

    public Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken)
    {
        return dbContext.AssetProducts.AnyAsync(
            product => product.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<AssetProduct>> GetProductsForAvailabilityAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.AssetProducts
            .AsNoTracking()
            .Include(product => product.Category)
            .OrderBy(product => product.Category!.CategoryName)
            .ThenBy(product => product.ProductName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockBalanceData>> GetBulkBalancesAsync(
        CancellationToken cancellationToken)
    {
        var receiptBalances = await dbContext.StockMovements
            .Where(movement =>
                movement.MovementType == MovementType.Receipt
                && movement.Product!.TrackingType == TrackingType.Bulk)
            .GroupBy(movement => movement.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(movement =>
                    movement.ReceiptItem == null
                    || movement.ReceiptItem.Condition != AssetCondition.Damaged
                        ? movement.Quantity
                        : 0),
                Damaged = group.Sum(movement =>
                    movement.ReceiptItem != null
                    && movement.ReceiptItem.Condition == AssetCondition.Damaged
                        ? movement.Quantity
                        : 0),
            })
            .ToListAsync(cancellationToken);
        var assignmentBalances = await dbContext.StockMovements
            .Where(movement =>
                movement.MovementType == MovementType.Assignment
                && movement.Product!.TrackingType == TrackingType.Bulk)
            .GroupBy(movement => movement.ProductId)
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(movement => movement.Quantity) })
            .ToListAsync(cancellationToken);
        var returnBalances = await dbContext.StockMovements
            .Where(movement =>
                movement.MovementType == MovementType.Return
                && movement.Product!.TrackingType == TrackingType.Bulk)
            .GroupBy(movement => movement.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(movement =>
                    movement.ReceiptItem == null
                    || movement.ReceiptItem.Condition != AssetCondition.Damaged
                        && (!movement.ReferenceId.HasValue
                            || dbContext.AssetReturns.Any(assetReturn =>
                                assetReturn.ReturnId == movement.ReferenceId.Value
                                && assetReturn.ReturnedCondition != AssetCondition.Damaged))
                        ? movement.Quantity
                        : 0),
                TotalQuantity = group.Sum(movement => movement.Quantity),
                Damaged = group.Sum(movement =>
                    movement.ReferenceId.HasValue
                    && dbContext.AssetReturns.Any(assetReturn =>
                        assetReturn.ReturnId == movement.ReferenceId.Value
                        && assetReturn.ReturnedCondition == AssetCondition.Damaged)
                        ? movement.Quantity
                        : 0),
            })
            .ToListAsync(cancellationToken);
        var adjustmentBalances = await (
                from movement in dbContext.StockMovements
                join adjustment in dbContext.StockAdjustments
                    on movement.ReferenceId equals (int?)adjustment.AdjustmentId
                where movement.MovementType == MovementType.Adjustment
                    && movement.Product!.TrackingType == TrackingType.Bulk
                group adjustment by movement.ProductId into adjustments
                select new
                {
                    ProductId = adjustments.Key,
                    Quantity = adjustments.Sum(item => item.QuantityChange),
                })
            .ToListAsync(cancellationToken);
        var productIds = receiptBalances.Select(item => item.ProductId)
            .Concat(assignmentBalances.Select(item => item.ProductId))
            .Concat(returnBalances.Select(item => item.ProductId))
            .Concat(adjustmentBalances.Select(item => item.ProductId))
            .Distinct();

        return productIds.Select(productId =>
        {
            var receipts = receiptBalances.SingleOrDefault(item => item.ProductId == productId);
            var totalAssignments = assignmentBalances
                .SingleOrDefault(item => item.ProductId == productId)?.Quantity ?? 0;
            var returns = returnBalances.SingleOrDefault(item => item.ProductId == productId);
            var assignments = Math.Max(0, totalAssignments - (returns?.TotalQuantity ?? 0));
            var adjustments = adjustmentBalances.SingleOrDefault(item => item.ProductId == productId)?.Quantity ?? 0;
            var available = (receipts?.Quantity ?? 0) + (returns?.Quantity ?? 0)
                - totalAssignments + adjustments;
            var damaged = (receipts?.Damaged ?? 0) + (returns?.Damaged ?? 0);
            return new StockBalanceData(productId, available, assignments, damaged, damaged);
        }).ToArray();
    }

    public async Task<StockBalanceData?> GetBulkBalanceAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        var balances = await GetBulkBalancesAsync(cancellationToken);
        return balances.SingleOrDefault(item => item.ProductId == productId);
    }

    public async Task<IReadOnlyList<StockBalanceData>> GetUnitBalancesAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.AssetUnits
            .Where(unit => unit.Product!.TrackingType == TrackingType.Individual)
            .GroupBy(unit => unit.ProductId)
            .Select(group => new StockBalanceData(
                group.Key,
                group.Count(unit =>
                    unit.Status == AssetStatus.Available && unit.Condition != AssetCondition.Damaged),
                group.Count(unit => unit.Status == AssetStatus.Assigned),
                group.Count(unit =>
                    unit.Status == AssetStatus.UnderRepair
                    || unit.Status == AssetStatus.Retired
                    || unit.Condition == AssetCondition.Damaged
                    && unit.Status != AssetStatus.Assigned),
                group.Count(unit => unit.Condition == AssetCondition.Damaged)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AssetUnit>> GetProductUnitsAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AssetUnits
            .AsNoTracking()
            .Where(unit => unit.ProductId == productId)
            .OrderBy(unit => unit.AssetCode)
            .ToListAsync(cancellationToken);
    }

    public Task<AssetUnit?> GetAssetUnitByCodeAsync(
        string assetCode,
        CancellationToken cancellationToken)
    {
        return dbContext.AssetUnits
            .AsNoTracking()
            .Include(unit => unit.Product)
                .ThenInclude(product => product!.Category)
            .Include(unit => unit.AssignmentItems)
                .ThenInclude(item => item.Assignment)
                    .ThenInclude(assignment => assignment!.Employee)
            .Include(unit => unit.AssignmentItems)
                .ThenInclude(item => item.Returns)
            .SingleOrDefaultAsync(unit => unit.AssetCode == assetCode, cancellationToken);
    }

    public async Task<int> ConvertBulkStockToIndividualAsync(
        int productId,
        string? actor,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction? transaction =
                dbContext.Database.CurrentTransaction is null
                    ? await dbContext.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable, cancellationToken)
                    : null;
        var product = await dbContext.AssetProducts
                .SingleOrDefaultAsync(item => item.ProductId == productId, cancellationToken);
        if (product is null)
        {
            throw new BulkStockConversionException("The selected product does not exist.");
        }

        if (product.TrackingType != TrackingType.Bulk)
        {
            throw new BulkStockConversionException(
                "This product is already tracked as individual assets.");
        }

        var balance = await GetBulkBalanceAsync(productId, cancellationToken);
        if (balance is null || balance.AvailableQuantity <= 0)
        {
            throw new BulkStockConversionException(
                "There is no available bulk stock to convert into individually barcoded assets.");
        }

        if (balance.AssignedQuantity > 0)
        {
            throw new BulkStockConversionException(
                "Return all assigned bulk stock before converting this product to individual assets.");
        }

        if (balance.UnavailableQuantity > 0 || balance.DamagedQuantity > 0)
        {
            throw new BulkStockConversionException(
                "Resolve damaged or unavailable bulk stock before converting this product.");
        }

        if (await dbContext.AssetUnits.AnyAsync(
                    unit => unit.ProductId == productId, cancellationToken))
        {
            throw new BulkStockConversionException(
                "This bulk product already has asset-unit records and cannot be converted safely.");
        }

        var receiptMovements = await dbContext.StockMovements
                .Where(movement => movement.ProductId == productId
                    && movement.MovementType == MovementType.Receipt
                    && movement.ReceiptItemId.HasValue)
                .GroupBy(movement => movement.ReceiptItemId!.Value)
                .Select(group => new
                {
                    ReceiptItemId = group.Key,
                    Quantity = group.Sum(movement => movement.Quantity),
                })
                .ToListAsync(cancellationToken);
        var assignmentMovements = await dbContext.StockMovements
                .Where(movement => movement.ProductId == productId
                    && movement.MovementType == MovementType.Assignment
                    && movement.ReceiptItemId.HasValue)
                .GroupBy(movement => movement.ReceiptItemId!.Value)
                .Select(group => new
                {
                    ReceiptItemId = group.Key,
                    Quantity = group.Sum(movement => movement.Quantity),
                })
                .ToListAsync(cancellationToken);
        var returnMovements = await dbContext.StockMovements
                .Where(movement => movement.ProductId == productId
                    && movement.MovementType == MovementType.Return
                    && movement.ReceiptItemId.HasValue
                    && movement.ReceiptItem!.Condition != AssetCondition.Damaged
                    && (!movement.ReferenceId.HasValue
                        || dbContext.AssetReturns.Any(assetReturn =>
                            assetReturn.ReturnId == movement.ReferenceId.Value
                            && assetReturn.ReturnedCondition != AssetCondition.Damaged)))
                .GroupBy(movement => movement.ReceiptItemId!.Value)
                .Select(group => new
                {
                    ReceiptItemId = group.Key,
                    Quantity = group.Sum(movement => movement.Quantity),
                })
                .ToListAsync(cancellationToken);

        var assignmentQuantities = assignmentMovements.ToDictionary(
                movement => movement.ReceiptItemId, movement => movement.Quantity);
        var returnQuantities = returnMovements.ToDictionary(
                movement => movement.ReceiptItemId, movement => movement.Quantity);
        var receiptItemIds = receiptMovements
                .Select(movement => movement.ReceiptItemId)
                .ToArray();
        var receiptItems = await dbContext.StockReceiptItems
                .Include(item => item.Receipt)
                .Where(item => receiptItemIds.Contains(item.ReceiptItemId)
                    && item.Condition != AssetCondition.Damaged)
                .ToListAsync(cancellationToken);
        var batches = receiptItems
                .Select(item =>
                {
                    var received = receiptMovements.Single(movement =>
                        movement.ReceiptItemId == item.ReceiptItemId).Quantity;
                    var assigned = assignmentQuantities.GetValueOrDefault(item.ReceiptItemId);
                    var returned = returnQuantities.GetValueOrDefault(item.ReceiptItemId);
                    return new
                    {
                        Item = item,
                        AvailableQuantity = received + returned - assigned,
                    };
                })
                .Where(batch => batch.AvailableQuantity > 0)
                .OrderBy(batch => batch.Item.Receipt!.ReceivedDate)
                .ThenBy(batch => batch.Item.ReceiptItemId)
                .ToArray();
        if (batches.Sum(batch => batch.AvailableQuantity) != balance.AvailableQuantity)
        {
            throw new BulkStockConversionException(
                "Available stock does not match its receipt history. Reconcile stock adjustments before conversion.");
        }

        var normalizedActor = string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim();
        if (normalizedActor.Length > 100)
        {
            normalizedActor = normalizedActor[..100];
        }

        var units = new List<(AssetUnit Unit, StockReceiptItem ReceiptItem, DateTime ReceivedDate)>(
                balance.AvailableQuantity);
        foreach (var batch in batches)
        {
            for (var i = 0; i < batch.AvailableQuantity; i++)
            {
                var unit = new AssetUnit
                {
                    ProductId = productId,
                    Product = product,
                    AssetCode = $"PENDING-{Guid.NewGuid():N}",
                    PurchaseDate = DateTime.SpecifyKind(batch.Item.PurchaseDate.Date, DateTimeKind.Utc),
                    WarrantyExpiryDate = batch.Item.WarrantyExpiryDate.HasValue
                        ? DateTime.SpecifyKind(batch.Item.WarrantyExpiryDate.Value.Date, DateTimeKind.Utc)
                        : null,
                    PurchaseCost = batch.Item.UnitPrice,
                    Status = AssetStatus.Available,
                    Condition = batch.Item.Condition,
                    CreatedBy = normalizedActor,
                };
                dbContext.AssetUnits.Add(unit);
                units.Add((unit, batch.Item, batch.Item.Receipt!.ReceivedDate));
            }
        }

        product.TrackingType = TrackingType.Individual;
        product.ModifiedBy = normalizedActor;
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var (unit, receiptItem, receivedDate) in units)
        {
            unit.AssetCode = $"AST-{unit.AssetUnitId:D6}";
            dbContext.StockMovements.Add(new StockMovement
            {
                ProductId = productId,
                ReceiptItemId = receiptItem.ReceiptItemId,
                AssetUnitId = unit.AssetUnitId,
                MovementType = MovementType.Adjustment,
                Quantity = 1,
                MovementDate = DateTime.SpecifyKind(receivedDate, DateTimeKind.Utc),
                ReferenceId = receiptItem.ReceiptId,
                Remarks = StockMovement.BulkTrackingConversionRemarks,
                CreatedBy = normalizedActor,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return units.Count;
    }

    public async Task<(IReadOnlyList<StockMovement> Movements, int TotalCount)> GetMovementsAsync(
        StockMovementFilter filter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.StockMovements
            .AsNoTracking()
            .Include(movement => movement.Product)
            .Include(movement => movement.AssetUnit)
            .AsQueryable();
        if (filter.ProductId.HasValue)
        {
            query = query.Where(movement => movement.ProductId == filter.ProductId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.MovementType)
            && Enum.TryParse<MovementType>(filter.MovementType, true, out var movementType))
        {
            query = query.Where(movement => movement.MovementType == movementType);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(movement => movement.MovementDate >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(movement =>
                movement.MovementDate < filter.EndDate.Value.AddDays(1));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var movements = await query
            .OrderByDescending(movement => movement.MovementDate)
            .ThenByDescending(movement => movement.MovementId)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return (movements, totalCount);
    }

    private IQueryable<StockReceipt> ReceiptQuery()
    {
        return dbContext.StockReceipts
            .Include(receipt => receipt.Items)
            .ThenInclude(item => item.Product)
            .ThenInclude(product => product!.Category)
            .Include(receipt => receipt.Items)
            .ThenInclude(item => item.StockMovements)
            .ThenInclude(movement => movement.AssetUnit);
    }

    private static IOrderedQueryable<StockReceipt> OrderReceipts(
        IQueryable<StockReceipt> query,
        string sortBy,
        string direction)
    {
        var descending = direction == "desc";
        return sortBy switch
        {
            "receiptnumber" => descending
                ? query.OrderByDescending(receipt => receipt.ReceiptNumber).ThenByDescending(receipt => receipt.ReceiptId)
                : query.OrderBy(receipt => receipt.ReceiptNumber).ThenBy(receipt => receipt.ReceiptId),
            "productname" => descending
                ? query.OrderByDescending(receipt => receipt.Items.Select(item => item.Product!.ProductName).FirstOrDefault())
                    .ThenByDescending(receipt => receipt.ReceiptId)
                : query.OrderBy(receipt => receipt.Items.Select(item => item.Product!.ProductName).FirstOrDefault())
                    .ThenBy(receipt => receipt.ReceiptId),
            "trackingtype" => descending
                ? query.OrderByDescending(receipt => receipt.Items.Select(item => item.Product!.TrackingType).FirstOrDefault())
                    .ThenByDescending(receipt => receipt.ReceiptId)
                : query.OrderBy(receipt => receipt.Items.Select(item => item.Product!.TrackingType).FirstOrDefault())
                    .ThenBy(receipt => receipt.ReceiptId),
            "quantity" => descending
                ? query.OrderByDescending(receipt => receipt.Items.Sum(item => item.Quantity)).ThenByDescending(receipt => receipt.ReceiptId)
                : query.OrderBy(receipt => receipt.Items.Sum(item => item.Quantity)).ThenBy(receipt => receipt.ReceiptId),
            "supplier" => descending
                ? query.OrderByDescending(receipt => receipt.Supplier).ThenByDescending(receipt => receipt.ReceiptId)
                : query.OrderBy(receipt => receipt.Supplier).ThenBy(receipt => receipt.ReceiptId),
            "invoicenumber" => descending
                ? query.OrderByDescending(receipt => receipt.InvoiceNumber).ThenByDescending(receipt => receipt.ReceiptId)
                : query.OrderBy(receipt => receipt.InvoiceNumber).ThenBy(receipt => receipt.ReceiptId),
            "totalamount" => descending
                ? query.OrderByDescending(receipt => receipt.Items.Sum(item => item.Quantity * item.UnitPrice))
                    .ThenByDescending(receipt => receipt.ReceiptId)
                : query.OrderBy(receipt => receipt.Items.Sum(item => item.Quantity * item.UnitPrice))
                    .ThenBy(receipt => receipt.ReceiptId),
            "receivedby" => descending
                ? query.OrderByDescending(receipt => receipt.CreatedBy).ThenByDescending(receipt => receipt.ReceiptId)
                : query.OrderBy(receipt => receipt.CreatedBy).ThenBy(receipt => receipt.ReceiptId),
            _ => descending
                ? query.OrderByDescending(receipt => receipt.ReceivedDate).ThenByDescending(receipt => receipt.ReceiptId)
                : query.OrderBy(receipt => receipt.ReceivedDate).ThenBy(receipt => receipt.ReceiptId),
        };
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}
