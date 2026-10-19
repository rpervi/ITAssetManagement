using System.ComponentModel.DataAnnotations;

namespace ITAssetManagement.Application.DTOs;

public sealed record StockProductDto(
    int ProductId,
    int CategoryId,
    string CategoryName,
    string ProductName,
    string Brand,
    string? Model,
    string TrackingType);

public sealed record IndividualStockAssetRequest(
    [property: StringLength(100)] string? SerialNumber,
    [property: Required] DateTime PurchaseDate,
    DateTime? WarrantyExpiryDate,
    [property: Range(typeof(decimal), "0", "9999999999999999.99")] decimal PurchaseCost,
    [property: Required, StringLength(30)] string Condition);

public sealed record ReceiveIndividualStockRequest(
    [property: Required] Guid IdempotencyKey,
    [property: Range(1, int.MaxValue)] int ProductId,
    [property: Required, MinLength(1), MaxLength(200)]
    IReadOnlyList<IndividualStockAssetRequest> Assets,
    [property: StringLength(200)] string? Supplier,
    [property: StringLength(100)] string? InvoiceNumber,
    [property: StringLength(1000)] string? Remarks);

public sealed record ReceiveBulkStockRequest(
    [property: Required] Guid IdempotencyKey,
    [property: Range(1, int.MaxValue)] int ProductId,
    [property: Range(1, int.MaxValue)] int Quantity,
    [property: Range(typeof(decimal), "0", "9999999999999999.99")] decimal UnitPrice,
    [property: Required] DateTime PurchaseDate,
    DateTime? WarrantyExpiryDate,
    [property: Required, StringLength(30)] string Condition,
    [property: StringLength(200)] string? Supplier,
    [property: StringLength(100)] string? InvoiceNumber,
    [property: StringLength(1000)] string? Remarks);

public sealed record StockReceiptItemResponse(
    int ReceiptItemId,
    int ProductId,
    string ProductName,
    string Brand,
    string? Model,
    int Quantity,
    decimal UnitPrice,
    DateTime PurchaseDate,
    DateTime? WarrantyExpiryDate,
    string Condition,
    IReadOnlyList<StockAssetUnitResponse> Assets);

public sealed record StockAssetUnitResponse(
    int AssetUnitId,
    string AssetCode,
    string? SerialNumber,
    DateTime PurchaseDate,
    DateTime? WarrantyExpiryDate,
    decimal PurchaseCost,
    string Status,
    string Condition);

public sealed record StockReceiptResponse(
    int ReceiptId,
    string ReceiptNumber,
    DateTime ReceivedDate,
    string? Supplier,
    string? InvoiceNumber,
    string? Remarks,
    string? ReceivedBy,
    string TrackingType,
    int Quantity,
    decimal TotalAmount,
    IReadOnlyList<StockReceiptItemResponse> Items);

public sealed record StockReceiptSummaryResponse(
    int ReceiptId,
    string ReceiptNumber,
    DateTime ReceivedDate,
    string ProductName,
    string TrackingType,
    int Quantity,
    string? Supplier,
    string? InvoiceNumber,
    decimal TotalAmount,
    string? ReceivedBy);

public sealed record PagedStockReceiptsResponse(
    IReadOnlyList<StockReceiptSummaryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record StockAvailabilityResponse(
    int ProductId,
    string ProductName,
    string CategoryName,
    string TrackingType,
    int AvailableQuantity,
    int AssignedQuantity,
    int UnavailableQuantity,
    int DamagedQuantity,
    int MinimumStock);

public sealed record StockMovementResponse(
    int MovementId,
    int ProductId,
    string ProductName,
    int? ReceiptItemId,
    int? AssetUnitId,
    string? AssetCode,
    string MovementType,
    int Quantity,
    DateTime MovementDate,
    int? ReferenceId,
    string? Remarks);

public sealed record StockBalanceData(
    int ProductId,
    int AvailableQuantity,
    int AssignedQuantity,
    int UnavailableQuantity,
    int DamagedQuantity);

public sealed record StockReceiptFilter(
    string? Search,
    int? ProductId,
    DateTime? StartDate,
    DateTime? EndDate,
    int Page,
    int PageSize,
    string SortBy,
    string SortDirection);

public sealed record StockMovementFilter(int? ProductId, int Page, int PageSize);

public sealed record PagedStockMovementsResponse(
    IReadOnlyList<StockMovementResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record StockReceiptSaveResult(StockReceiptResponse Receipt, bool Created);
