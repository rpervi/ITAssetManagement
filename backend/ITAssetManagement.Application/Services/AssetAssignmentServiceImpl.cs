using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Domain.Entities;

namespace ITAssetManagement.Application.Services;

public sealed class AssetAssignmentServiceImpl(IAssetAssignmentRepository repository)
    : IAssetAssignmentService
{
    public async Task<AssetAssignmentPageResponse> GetPageAsync(
        string? search,
        int? employeeId,
        int? productId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken,
        bool returnableOnly = false)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new InvalidAssetAssignmentException(
                "Page must be positive and page size must be between 1 and 100.");
        }

        if (employeeId is <= 0 || productId is <= 0)
        {
            throw new InvalidAssetAssignmentException("Employee and product IDs must be positive.");
        }

        if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
        {
            throw new InvalidAssetAssignmentException("Start date cannot be after end date.");
        }

        var filter = new AssetAssignmentListFilter(
            NormalizeOptional(search),
            employeeId,
            productId,
            startDate?.Date,
            endDate?.Date,
            page,
            pageSize,
            NormalizeSortBy(sortBy),
            string.Equals(sortDirection?.Trim(), "desc", StringComparison.OrdinalIgnoreCase),
            returnableOnly);
        var result = await repository.GetPageAsync(filter, cancellationToken);
        return new AssetAssignmentPageResponse(
            result.Items.Select(ToResponse).ToArray(),
            result.TotalCount,
            page,
            pageSize);
    }

    public async Task<AssetAssignmentResponse?> GetByIdAsync(
        int assignmentId,
        CancellationToken cancellationToken)
    {
        ValidateId(assignmentId);
        var assignment = await repository.GetByIdAsync(assignmentId, cancellationToken);
        return assignment is null ? null : ToResponse(assignment);
    }

    public async Task<IReadOnlyList<AssetAssignmentResponse>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken)
    {
        ValidateId(employeeId);
        var assignments = await repository.GetByEmployeeAsync(employeeId, cancellationToken);
        return assignments.Select(ToResponse).ToArray();
    }

    public async Task<IReadOnlyList<AssetAssignmentProductDto>> GetActiveProductsAsync(
        CancellationToken cancellationToken)
    {
        var products = await repository.GetActiveProductsAsync(cancellationToken);
        return products.Select(product => new AssetAssignmentProductDto(
            product.ProductId,
            product.ProductName,
            product.CategoryId,
            product.Category?.CategoryName ?? string.Empty,
            product.Brand ?? string.Empty,
            product.Model,
            product.TrackingType.ToString())).ToArray();
    }

    public async Task<IReadOnlyList<AvailableAssetUnitResponse>?> GetAvailableUnitsAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        ValidateId(productId);
        var product = await repository.GetProductAsync(productId, cancellationToken);
        if (product is null || !product.IsActive || product.Category?.IsActive != true
            || product.TrackingType != TrackingType.Individual)
        {
            return null;
        }

        var units = await repository.GetAvailableUnitsAsync(productId, cancellationToken);
        return units.Select(unit => new AvailableAssetUnitResponse(
            unit.AssetUnitId,
            unit.AssetCode,
            unit.SerialNumber,
            unit.WarrantyExpiryDate,
            unit.Condition.ToString())).ToArray();
    }

    public async Task<AvailableStockResponse?> GetAvailableStockAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        ValidateId(productId);
        var product = await repository.GetProductAsync(productId, cancellationToken);
        if (product is null || !product.IsActive || product.Category?.IsActive != true
            || product.TrackingType != TrackingType.Bulk)
        {
            return null;
        }

        var batches = await repository.GetAvailableBatchesAsync(productId, cancellationToken);
        var batchResponses = batches.Select(batch => new AvailableStockBatchResponse(
            batch.ReceiptItemId,
            batch.ReceiptNumber,
            batch.ReceivedDate,
            batch.PurchaseDate,
            batch.WarrantyExpiryDate,
            batch.UnitPrice,
            batch.Condition.ToString(),
            batch.AvailableQuantity)).ToArray();
        return new AvailableStockResponse(
            product.ProductId,
            product.ProductName,
            product.Category?.CategoryName ?? string.Empty,
            product.Brand ?? string.Empty,
            product.Model,
            batchResponses.Sum(batch => batch.AvailableQuantity),
            batchResponses);
    }

    public async Task<AssetAssignmentSaveResult> CreateAsync(
        CreateAssetAssignmentRequest request,
        string? actor,
        CancellationToken cancellationToken)
    {
        if (request.IdempotencyKey == Guid.Empty)
        {
            throw new InvalidAssetAssignmentException("A non-empty idempotency key is required.");
        }

        ValidateId(request.EmployeeId);
        ValidateId(request.ProductId);
        if (request.AssignedDate == default)
        {
            throw new InvalidAssetAssignmentException("Assigned date is required.");
        }

        var employee = await repository.GetEmployeeAsync(request.EmployeeId, cancellationToken);
        if (employee is null || !employee.IsActive)
        {
            throw new InvalidAssetAssignmentException("Select an active employee.");
        }

        var product = await repository.GetProductAsync(request.ProductId, cancellationToken);
        if (product is null || !product.IsActive || product.Category?.IsActive != true)
        {
            throw new InvalidAssetAssignmentException("Select an active product and category.");
        }

        var normalizedActor = string.IsNullOrWhiteSpace(actor) ? "system" : actor.Trim();
        var assignment = new AssetAssignment
        {
            IdempotencyKey = request.IdempotencyKey,
            EmployeeId = employee.EmployeeId,
            AssignedDate = DateTime.SpecifyKind(request.AssignedDate.Date, DateTimeKind.Utc),
            AssignedBy = normalizedActor.Length <= 100 ? normalizedActor : normalizedActor[..100],
            Remarks = NormalizeOptional(request.Remarks),
            CreatedBy = normalizedActor,
        };

        var individual = product.TrackingType == TrackingType.Individual;
        if (individual)
        {
            var unitIds = request.AssetUnitIds?.ToArray() ?? [];
            if (unitIds.Length == 0 || unitIds.Any(id => id <= 0)
                || unitIds.Distinct().Count() != unitIds.Length)
            {
                throw new InvalidAssetAssignmentException(
                    "Select one or more unique, valid individual assets.");
            }

            if (request.Quantity != 0 && request.Quantity != unitIds.Length)
            {
                throw new InvalidAssetAssignmentException(
                    "Quantity must match the number of selected individual assets.");
            }

            assignment.Items = unitIds.Select(unitId => new AssetAssignmentItem
            {
                ProductId = product.ProductId,
                AssetUnitId = unitId,
                Quantity = 1,
                CreatedBy = normalizedActor,
            }).ToList();
        }
        else
        {
            if (request.AssetUnitIds is { Count: > 0 })
            {
                throw new InvalidAssetAssignmentException(
                    "Asset units can only be selected for individually tracked products.");
            }

            if (request.Quantity < 1)
            {
                throw new InvalidAssetAssignmentException("Bulk assignment quantity must be positive.");
            }

            assignment.Items.Add(new AssetAssignmentItem
            {
                ProductId = product.ProductId,
                Quantity = request.Quantity,
                CreatedBy = normalizedActor,
            });
        }

        var (saved, created) = await repository.CreateAsync(
            assignment,
            individual,
            cancellationToken);
        return new AssetAssignmentSaveResult(ToResponse(saved), created);
    }

    public async Task<AssetAssignmentResponse> ReturnAsync(
        int assignmentId,
        ReturnAssetAssignmentRequest request,
        string? actor,
        CancellationToken cancellationToken)
    {
        ValidateId(assignmentId);
        ValidateId(request.AssignmentItemId);
        if (request.Quantity < 1)
        {
            throw new InvalidAssetAssignmentException("Return quantity must be positive.");
        }

        if (request.ReturnDate == default)
        {
            throw new InvalidAssetAssignmentException("Return date is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ReturnedCondition)
            || !Enum.TryParse<AssetCondition>(
                request.ReturnedCondition.Trim(),
                ignoreCase: true,
                out var returnedCondition)
            || !Enum.IsDefined(returnedCondition))
        {
            throw new InvalidAssetAssignmentException("Select a valid returned condition.");
        }

        var assignment = await repository.ReturnAsync(
            assignmentId,
            request with { ReturnedCondition = returnedCondition.ToString() },
            returnedCondition,
            actor,
            cancellationToken);
        return ToResponse(assignment);
    }

    private static AssetAssignmentResponse ToResponse(AssetAssignment assignment)
    {
        var items = assignment.Items.Select(item =>
        {
            var returned = item.Returns.Sum(assetReturn => assetReturn.ReturnQuantity);
            var batchAllocations = item.BatchAllocations.Select(allocation =>
                new AssetAssignmentBatchAllocationResponse(
                    allocation.ReceiptItemId,
                    allocation.ReceiptItem?.Receipt?.ReceiptNumber ?? string.Empty,
                    allocation.ReceiptItem?.Receipt?.ReceivedDate ?? default,
                    allocation.ReceiptItem?.PurchaseDate ?? default,
                    allocation.ReceiptItem?.WarrantyExpiryDate,
                    allocation.ReceiptItem?.UnitPrice ?? 0,
                    allocation.ReceiptItem?.Condition.ToString() ?? string.Empty,
                    allocation.Quantity)).ToArray();
            return new AssetAssignmentItemResponse(
                item.AssignmentItemId,
                item.ProductId,
                item.Product?.ProductName ?? string.Empty,
                item.Product?.Category?.CategoryName ?? string.Empty,
                item.Product?.Brand ?? string.Empty,
                item.Product?.Model,
                item.Product?.TrackingType.ToString() ?? string.Empty,
                item.Quantity,
                item.AssetUnit?.AssetCode,
                item.AssetUnit?.SerialNumber,
                item.AssetUnit?.WarrantyExpiryDate,
                batchAllocations,
                returned,
                Math.Max(0, item.Quantity - returned));
        }).ToArray();

        var returnedTotal = items.Sum(item => item.ReturnedQuantity);
        var issuedTotal = items.Sum(item => item.Quantity);
        var status = returnedTotal == 0
            ? "Active"
            : returnedTotal >= issuedTotal
                ? "Returned"
                : "Partially Returned";
        return new AssetAssignmentResponse(
            assignment.AssignmentId,
            $"ASN-{assignment.AssignmentId:D6}",
            assignment.EmployeeId,
            assignment.Employee?.EmployeeCode ?? string.Empty,
            assignment.Employee?.EmployeeName ?? string.Empty,
            assignment.Employee?.Department ?? string.Empty,
            assignment.AssignedDate,
            assignment.AssignedBy,
            assignment.Remarks,
            issuedTotal,
            status,
            items);
    }

    private static string NormalizeSortBy(string? sortBy) =>
        sortBy?.Trim().ToLowerInvariant() switch
        {
            "employee" or "employeename" => "employee",
            "assigneddate" or "date" => "assigneddate",
            "assignmentnumber" or "assignmentid" => "assignmentid",
            _ => "assigneddate",
        };

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateId(int id)
    {
        if (id <= 0)
        {
            throw new InvalidAssetAssignmentException("ID must be positive.");
        }
    }
}
