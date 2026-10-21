using ITAssetManagement.Application.DTOs;

namespace ITAssetManagement.Application.Services;

public interface IStockService
{
    Task<IReadOnlyList<StockProductDto>> GetProductsAsync(CancellationToken cancellationToken);
    Task<StockReceiptSaveResult> ReceiveIndividualAsync(
        ReceiveIndividualStockRequest request,
        string? actor,
        CancellationToken cancellationToken);
    Task<StockReceiptSaveResult> ReceiveBulkAsync(
        ReceiveBulkStockRequest request,
        string? actor,
        CancellationToken cancellationToken);
    Task<PagedStockReceiptsResponse> GetReceiptsAsync(
        string? search,
        int? productId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken);
    Task<StockReceiptResponse?> GetReceiptAsync(int receiptId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StockAvailabilityResponse>> GetAvailabilityAsync(CancellationToken cancellationToken);
    Task<StockAvailabilityResponse?> GetAvailabilityAsync(
        int productId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<StockAssetUnitResponse>?> GetProductUnitsAsync(
        int productId,
        CancellationToken cancellationToken);
    Task<AssetBarcodeDetailsResponse?> GetAssetByCodeAsync(
        string assetCode,
        CancellationToken cancellationToken);
    Task<BulkStockConversionResponse> ConvertBulkStockToIndividualAsync(
        int productId,
        string? actor,
        CancellationToken cancellationToken);
    Task<PagedStockMovementsResponse> GetMovementsAsync(
        int? productId,
        string? movementType,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
