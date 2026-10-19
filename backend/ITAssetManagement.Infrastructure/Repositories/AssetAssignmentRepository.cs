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

public sealed class AssetAssignmentRepository(AssetManagementDbContext dbContext)
    : IAssetAssignmentRepository
{
    public async Task<AssetAssignmentListResult> GetPageAsync(
        AssetAssignmentListFilter filter,
        CancellationToken cancellationToken)
    {
        var query = AssignmentQuery().AsNoTracking();
        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(assignment => assignment.EmployeeId == filter.EmployeeId.Value);
        }

        if (filter.ProductId.HasValue)
        {
            query = query.Where(assignment =>
                assignment.Items.Any(item => item.ProductId == filter.ProductId.Value));
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(assignment => assignment.AssignedDate >= filter.StartDate.Value);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(assignment =>
                assignment.AssignedDate < filter.EndDate.Value.AddDays(1));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(assignment =>
                assignment.Employee!.EmployeeCode.Contains(search)
                || assignment.Employee.EmployeeName.Contains(search)
                || assignment.AssignedBy.Contains(search)
                || assignment.Items.Any(item =>
                    item.Product!.ProductName.Contains(search)
                    || item.AssetUnit != null && item.AssetUnit.AssetCode.Contains(search)
                    || item.AssetUnit != null && item.AssetUnit.SerialNumber != null
                        && item.AssetUnit.SerialNumber.Contains(search)));
            if (int.TryParse(search, out var assignmentId))
            {
                query = query.Where(assignment =>
                    assignment.Employee!.EmployeeCode.Contains(search)
                    || assignment.Employee.EmployeeName.Contains(search)
                    || assignment.AssignedBy.Contains(search)
                    || assignment.Items.Any(item =>
                        item.Product!.ProductName.Contains(search)
                        || item.AssetUnit != null && item.AssetUnit.AssetCode.Contains(search)
                        || item.AssetUnit != null && item.AssetUnit.SerialNumber != null
                            && item.AssetUnit.SerialNumber.Contains(search))
                    || assignment.AssignmentId == assignmentId);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var ordered = filter.SortBy switch
        {
            "employee" => filter.SortDescending
                ? query.OrderByDescending(assignment => assignment.Employee!.EmployeeName)
                : query.OrderBy(assignment => assignment.Employee!.EmployeeName),
            "assignmentid" => filter.SortDescending
                ? query.OrderByDescending(assignment => assignment.AssignmentId)
                : query.OrderBy(assignment => assignment.AssignmentId),
            _ => filter.SortDescending
                ? query.OrderByDescending(assignment => assignment.AssignedDate)
                : query.OrderBy(assignment => assignment.AssignedDate),
        };

        var assignments = await ordered
            .ThenByDescending(assignment => assignment.AssignmentId)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return new AssetAssignmentListResult(assignments, totalCount);
    }

    public Task<AssetAssignment?> GetByIdAsync(
        int assignmentId,
        CancellationToken cancellationToken)
    {
        return AssignmentQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                assignment => assignment.AssignmentId == assignmentId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<AssetAssignment>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        return await AssignmentQuery()
            .AsNoTracking()
            .Where(assignment => assignment.EmployeeId == employeeId)
            .OrderByDescending(assignment => assignment.AssignedDate)
            .ThenByDescending(assignment => assignment.AssignmentId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AssetProduct>> GetActiveProductsAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.AssetProducts
            .AsNoTracking()
            .Include(product => product.Category)
            .Where(product => product.IsActive && product.Category != null
                && product.Category.IsActive)
            .OrderBy(product => product.ProductName)
            .ToListAsync(cancellationToken);
    }

    public Task<Employee?> GetEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        return dbContext.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(employee => employee.EmployeeId == employeeId, cancellationToken);
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

    public async Task<IReadOnlyList<AssetUnit>> GetAvailableUnitsAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AssetUnits
            .AsNoTracking()
            .Where(unit => unit.ProductId == productId
                && unit.Status == AssetStatus.Available
                && unit.Condition != AssetCondition.Damaged)
            .OrderBy(unit => unit.AssetCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AssetAssignmentBatchData>> GetAvailableBatchesAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        var receipts = await dbContext.StockMovements
            .AsNoTracking()
            .Where(movement => movement.ProductId == productId
                && movement.MovementType == MovementType.Receipt
                && movement.ReceiptItemId.HasValue)
            .GroupBy(movement => movement.ReceiptItemId!.Value)
            .Select(group => new { ReceiptItemId = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToListAsync(cancellationToken);
        var assignments = await dbContext.StockMovements
            .AsNoTracking()
            .Where(movement => movement.ProductId == productId
                && movement.MovementType == MovementType.Assignment
                && movement.ReceiptItemId.HasValue)
            .GroupBy(movement => movement.ReceiptItemId!.Value)
            .Select(group => new { ReceiptItemId = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToListAsync(cancellationToken);
        var returns = await dbContext.StockMovements
            .AsNoTracking()
            .Where(movement => movement.ProductId == productId
                && movement.MovementType == MovementType.Return
                && movement.ReceiptItemId.HasValue
                && movement.ReceiptItem!.Condition != AssetCondition.Damaged)
            .GroupBy(movement => movement.ReceiptItemId!.Value)
            .Select(group => new { ReceiptItemId = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToListAsync(cancellationToken);

        var assignmentQuantities = assignments.ToDictionary(item => item.ReceiptItemId, item => item.Quantity);
        var returnQuantities = returns.ToDictionary(item => item.ReceiptItemId, item => item.Quantity);
        var receiptItemIds = receipts.Select(item => item.ReceiptItemId).ToArray();
        if (receiptItemIds.Length == 0)
        {
            return [];
        }

        var receiptItems = await dbContext.StockReceiptItems
            .AsNoTracking()
            .Include(item => item.Receipt)
            .Where(item => receiptItemIds.Contains(item.ReceiptItemId)
                && item.Condition != AssetCondition.Damaged)
            .ToListAsync(cancellationToken);

        return receiptItems
            .Select(item =>
            {
                var receivedQuantity = receipts.Single(receipt =>
                    receipt.ReceiptItemId == item.ReceiptItemId).Quantity;
                var allocatedQuantity = assignmentQuantities.GetValueOrDefault(item.ReceiptItemId);
                var returnedQuantity = returnQuantities.GetValueOrDefault(item.ReceiptItemId);
                return new AssetAssignmentBatchData(
                    item.ReceiptItemId,
                    item.Receipt?.ReceiptNumber ?? string.Empty,
                    item.Receipt?.ReceivedDate ?? default,
                    item.PurchaseDate,
                    item.WarrantyExpiryDate,
                    item.UnitPrice,
                    item.Condition,
                    receivedQuantity + returnedQuantity - allocatedQuantity);
            })
            .Where(batch => batch.AvailableQuantity > 0)
            .OrderBy(batch => batch.ReceivedDate)
            .ThenBy(batch => batch.ReceiptItemId)
            .ToArray();
    }

    public async Task<(AssetAssignment Assignment, bool Created)> CreateAsync(
        AssetAssignment assignment,
        bool individual,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction? transaction =
            dbContext.Database.CurrentTransaction is null
                ? await dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken)
                : null;

        var existing = await AssignmentQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.IdempotencyKey == assignment.IdempotencyKey,
                cancellationToken);
        if (existing is not null)
        {
            EnsureSameRequest(existing, assignment);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return (existing, false);
        }

        var productId = assignment.Items.First().ProductId;
        var product = await dbContext.AssetProducts
            .FromSqlInterpolated(
                $"SELECT * FROM [dbo].[AssetProducts] WITH (UPDLOCK, HOLDLOCK) WHERE [ProductId] = {productId}")
            .Include(item => item.Category)
            .SingleOrDefaultAsync(cancellationToken);
        if (product is null || !product.IsActive || product.Category?.IsActive != true)
        {
            throw new InvalidAssetAssignmentException("The selected product is no longer active.");
        }

        var employee = await dbContext.Employees
            .SingleOrDefaultAsync(
                item => item.EmployeeId == assignment.EmployeeId,
                cancellationToken);
        if (employee is null || !employee.IsActive)
        {
            throw new InvalidAssetAssignmentException("The selected employee is no longer active.");
        }

        if (individual != (product.TrackingType == TrackingType.Individual))
        {
            throw new AssetAssignmentConflictException(
                "The product tracking type changed. Reload the product and try again.");
        }

        if (individual)
        {
            var unitIds = assignment.Items.Select(item => item.AssetUnitId!.Value).ToArray();
            var units = await dbContext.AssetUnits
                .Where(unit => unit.ProductId == productId && unitIds.Contains(unit.AssetUnitId))
                .ToListAsync(cancellationToken);
            if (units.Count != unitIds.Length
                || units.Any(unit => unit.Status != AssetStatus.Available
                    || unit.Condition == AssetCondition.Damaged))
            {
                throw new AssetAssignmentConflictException(
                    "One or more selected assets are no longer available. Refresh the asset list.");
            }

            foreach (var unit in units)
            {
                unit.Status = AssetStatus.Assigned;
                unit.ModifiedBy = assignment.CreatedBy;
            }
        }
        else
        {
            var item = assignment.Items.Single();
            var batches = await GetAvailableBatchesAsync(productId, cancellationToken);
            var remainingQuantity = item.Quantity;
            var allocations = new List<AssetAssignmentBatchAllocation>();
            foreach (var batch in batches)
            {
                if (remainingQuantity == 0)
                {
                    break;
                }

                var quantity = Math.Min(remainingQuantity, batch.AvailableQuantity);
                allocations.Add(new AssetAssignmentBatchAllocation
                {
                    AssignmentItem = item,
                    ReceiptItemId = batch.ReceiptItemId,
                    Quantity = quantity,
                    CreatedBy = assignment.CreatedBy,
                });
                remainingQuantity -= quantity;
            }

            if (remainingQuantity > 0)
            {
                throw new AssetAssignmentConflictException(
                    "There is not enough receipt-traceable stock available. Refresh inventory and try again.");
            }

            item.BatchAllocations = allocations;
        }

        dbContext.AssetAssignments.Add(assignment);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            if (individual)
            {
                foreach (var item in assignment.Items)
                {
                    var receiptItemId = await dbContext.StockMovements
                        .Where(movement => movement.AssetUnitId == item.AssetUnitId
                            && movement.MovementType == MovementType.Receipt)
                        .OrderByDescending(movement => movement.MovementDate)
                        .Select(movement => movement.ReceiptItemId)
                        .FirstOrDefaultAsync(cancellationToken);
                    dbContext.StockMovements.Add(new StockMovement
                    {
                        ProductId = productId,
                        ReceiptItemId = receiptItemId,
                        AssetUnitId = item.AssetUnitId,
                        MovementType = MovementType.Assignment,
                        Quantity = 1,
                        MovementDate = assignment.AssignedDate,
                        ReferenceId = item.AssignmentItemId,
                        Remarks = assignment.Remarks,
                        CreatedBy = assignment.CreatedBy,
                    });
                }
            }
            else
            {
                var item = assignment.Items.Single();
                foreach (var allocation in item.BatchAllocations)
                {
                    dbContext.StockMovements.Add(new StockMovement
                    {
                        ProductId = productId,
                        ReceiptItemId = allocation.ReceiptItemId,
                        MovementType = MovementType.Assignment,
                        Quantity = allocation.Quantity,
                        MovementDate = assignment.AssignedDate,
                        ReferenceId = item.AssignmentItemId,
                        Remarks = assignment.Remarks,
                        CreatedBy = assignment.CreatedBy,
                    });
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            var saved = await AssignmentQuery()
                .AsNoTracking()
                .SingleAsync(
                    item => item.AssignmentId == assignment.AssignmentId,
                    cancellationToken);
            return (saved, true);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            dbContext.ChangeTracker.Clear();
            var duplicate = await AssignmentQuery()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.IdempotencyKey == assignment.IdempotencyKey,
                    cancellationToken);
            if (duplicate is null)
            {
                throw;
            }

            EnsureSameRequest(duplicate, assignment);
            return (duplicate, false);
        }
    }

    private IQueryable<AssetAssignment> AssignmentQuery() =>
        dbContext.AssetAssignments
            .AsSplitQuery()
            .Include(assignment => assignment.Employee)
            .Include(assignment => assignment.Items)
                .ThenInclude(item => item.Product)
                    .ThenInclude(product => product!.Category)
            .Include(assignment => assignment.Items)
                .ThenInclude(item => item.AssetUnit)
            .Include(assignment => assignment.Items)
                .ThenInclude(item => item.Returns)
            .Include(assignment => assignment.Items)
                .ThenInclude(item => item.BatchAllocations)
                    .ThenInclude(allocation => allocation.ReceiptItem)
                        .ThenInclude(receiptItem => receiptItem!.Receipt);

    private static void EnsureSameRequest(
        AssetAssignment existing,
        AssetAssignment request)
    {
        var sameItems = existing.Items.Count == request.Items.Count
            && existing.Items.OrderBy(item => item.AssetUnitId)
                .ThenBy(item => item.ProductId)
                .ThenBy(item => item.Quantity)
                .Select(item => (item.ProductId, item.AssetUnitId, item.Quantity))
                .SequenceEqual(request.Items.OrderBy(item => item.AssetUnitId)
                    .ThenBy(item => item.ProductId)
                    .ThenBy(item => item.Quantity)
                    .Select(item => (item.ProductId, item.AssetUnitId, item.Quantity)));
        if (existing.EmployeeId != request.EmployeeId
            || existing.AssignedDate != request.AssignedDate
            || existing.Remarks != request.Remarks
            || !sameItems)
        {
            throw new AssetAssignmentIdempotencyConflictException(
                "This idempotency key was already used for a different assignment request.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
