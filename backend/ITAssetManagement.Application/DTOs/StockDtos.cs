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
    [param: StringLength(100)] string? SerialNumber,
    [param: Required] DateTime PurchaseDate,
    DateTime? WarrantyExpiryDate,
    [param: Range(typeof(decimal), "0", "9999999999999999.99")] decimal PurchaseCost,
    [param: Required, StringLength(30)] string Condition);

public sealed record ReceiveIndividualStockRequest(
    [param: Required] Guid IdempotencyKey,
    [param: Range(1, int.MaxValue)] int ProductId,
    [param: Required, MinLength(1), MaxLength(200)]
    IReadOnlyList<IndividualStockAssetRequest> Assets,
    [param: StringLength(200)] string? Supplier,
    [param: StringLength(100)] string? InvoiceNumber,
    [param: StringLength(1000)] string? Remarks);

public sealed record ReceiveBulkStockRequest(
    [param: Required] Guid IdempotencyKey,
    [param: Range(1, int.MaxValue)] int ProductId,
    [param: Range(1, int.MaxValue)] int Quantity,
    [param: Range(typeof(decimal), "0", "9999999999999999.99")] decimal UnitPrice,
    [param: Required] DateTime PurchaseDate,
    DateTime? WarrantyExpiryDate,
    [param: Required, StringLength(30)] string Condition,
    [param: StringLength(200)] string? Supplier,
    [param: StringLength(100)] string? InvoiceNumber,
    [param: StringLength(1000)] string? Remarks);

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

public sealed record AssetBarcodeDetailsResponse(
    int AssetUnitId,
    int ProductId,
    string AssetCode,
    string AssetName,
    string AssetType,
    string CategoryName,
    string Manufacturer,
    string? Model,
    string? SerialNumber,
    string? AssignedEmployee,
    string? Department,
    string? CurrentLocation,
    string Status,
    DateTime PurchaseDate);

public sealed record BulkStockConversionResponse(
    int ProductId,
    int AssetsCreated);

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

public sealed record StockMovementFilter(
    int? ProductId,
    string? MovementType,
    DateTime? StartDate,
    DateTime? EndDate,
    int Page,
    int PageSize);

public sealed record PagedStockMovementsResponse(
    IReadOnlyList<StockMovementResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record StockReceiptSaveResult(StockReceiptResponse Receipt, bool Created);
