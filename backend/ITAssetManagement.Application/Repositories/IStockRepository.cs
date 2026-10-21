using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Domain.Entities;

namespace ITAssetManagement.Application.Repositories;

public interface IStockRepository
{
    Task<IReadOnlyList<AssetProduct>> GetActiveProductsAsync(CancellationToken cancellationToken);
    Task<AssetProduct?> GetProductAsync(int productId, CancellationToken cancellationToken);
    Task<bool> SerialNumberExistsAsync(string normalizedSerialNumber, CancellationToken cancellationToken);
    Task<StockReceipt?> GetReceiptByNumberAsync(
        string receiptNumber,
        CancellationToken cancellationToken);
    Task<(StockReceipt Receipt, bool Created)> SaveReceiptAsync(
        StockReceipt receipt,
        CancellationToken cancellationToken);
    Task<(IReadOnlyList<StockReceipt> Receipts, int TotalCount)> GetReceiptsAsync(
        StockReceiptFilter filter,
        CancellationToken cancellationToken);
    Task<StockReceipt?> GetReceiptAsync(int receiptId, CancellationToken cancellationToken);
    Task<bool> ProductExistsAsync(int productId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AssetProduct>> GetProductsForAvailabilityAsync(
        CancellationToken cancellationToken);
    Task<IReadOnlyList<StockBalanceData>> GetBulkBalancesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<StockBalanceData>> GetUnitBalancesAsync(CancellationToken cancellationToken);
    Task<StockBalanceData?> GetBulkBalanceAsync(
        int productId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<AssetUnit>> GetProductUnitsAsync(
        int productId,
        CancellationToken cancellationToken);
    Task<AssetUnit?> GetAssetUnitByCodeAsync(
        string assetCode,
        CancellationToken cancellationToken);
    Task<int> ConvertBulkStockToIndividualAsync(
        int productId,
        string? actor,
        CancellationToken cancellationToken);
    Task<(IReadOnlyList<StockMovement> Movements, int TotalCount)> GetMovementsAsync(
        StockMovementFilter filter,
        CancellationToken cancellationToken);
}
