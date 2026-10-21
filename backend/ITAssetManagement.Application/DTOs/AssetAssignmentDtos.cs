using System.ComponentModel.DataAnnotations;

namespace ITAssetManagement.Application.DTOs;

public sealed record AssetAssignmentProductDto(
    int ProductId,
    string ProductName,
    int CategoryId,
    string CategoryName,
    string Brand,
    string? Model,
    string TrackingType);

public sealed record AvailableAssetUnitResponse(
    int AssetUnitId,
    string AssetCode,
    string? SerialNumber,
    DateTime? WarrantyExpiryDate,
    string Condition);

public sealed record AvailableStockBatchResponse(
    int ReceiptItemId,
    string ReceiptNumber,
    DateTime ReceivedDate,
    DateTime PurchaseDate,
    DateTime? WarrantyExpiryDate,
    decimal UnitPrice,
    string Condition,
    int AvailableQuantity);

public sealed record AssetAssignmentBatchAllocationResponse(
    int ReceiptItemId,
    string ReceiptNumber,
    DateTime ReceivedDate,
    DateTime PurchaseDate,
    DateTime? WarrantyExpiryDate,
    decimal UnitPrice,
    string Condition,
    int Quantity);

public sealed record AvailableStockResponse(
    int ProductId,
    string ProductName,
    string CategoryName,
    string Brand,
    string? Model,
    int AvailableQuantity,
    IReadOnlyList<AvailableStockBatchResponse> Batches);

public sealed record CreateAssetAssignmentRequest(
    Guid IdempotencyKey,
    [param: Range(1, int.MaxValue)] int EmployeeId,
    [param: Range(1, int.MaxValue)] int ProductId,
    IReadOnlyList<int>? AssetUnitIds,
    [param: Range(0, int.MaxValue)] int Quantity,
    [param: Required] DateTime AssignedDate,
    [param: StringLength(1000)] string? Remarks);

public sealed record ReturnAssetAssignmentRequest(
    [param: Range(1, int.MaxValue)] int AssignmentItemId,
    [param: Range(1, int.MaxValue)] int Quantity,
    [param: Required] DateTime ReturnDate,
    [param: Required, StringLength(30)] string ReturnedCondition,
    [param: StringLength(1000)] string? Remarks);

public sealed record AssetAssignmentItemResponse(
    int AssignmentItemId,
    int ProductId,
    string ProductName,
    string CategoryName,
    string Brand,
    string? Model,
    string TrackingType,
    int Quantity,
    string? AssetCode,
    string? SerialNumber,
    DateTime? WarrantyExpiryDate,
    IReadOnlyList<AssetAssignmentBatchAllocationResponse> BatchAllocations,
    int ReturnedQuantity,
    int OutstandingQuantity);

public sealed record AssetAssignmentResponse(
    int AssignmentId,
    string AssignmentNumber,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string Department,
    DateTime AssignedDate,
    string AssignedBy,
    string? Remarks,
    int IssuedQuantity,
    string Status,
    IReadOnlyList<AssetAssignmentItemResponse> Items);

public sealed record AssetAssignmentPageResponse(
    IReadOnlyList<AssetAssignmentResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record AssetAssignmentListFilter(
    string? Search,
    int? EmployeeId,
    int? ProductId,
    DateTime? StartDate,
    DateTime? EndDate,
    int Page,
    int PageSize,
    string SortBy,
    bool SortDescending,
    bool ReturnableOnly = false);

public sealed record AssetAssignmentSaveResult(AssetAssignmentResponse Assignment, bool Created);
