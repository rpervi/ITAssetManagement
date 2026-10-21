using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Domain.Entities;

namespace ITAssetManagement.Application.Repositories;

public sealed record AssetAssignmentListResult(
    IReadOnlyList<AssetAssignment> Items,
    int TotalCount);

public sealed record AssetAssignmentBatchData(
    int ReceiptItemId,
    string ReceiptNumber,
    DateTime ReceivedDate,
    DateTime PurchaseDate,
    DateTime? WarrantyExpiryDate,
    decimal UnitPrice,
    AssetCondition Condition,
    int AvailableQuantity);

public interface IAssetAssignmentRepository
{
    Task<AssetAssignmentListResult> GetPageAsync(
        AssetAssignmentListFilter filter,
        CancellationToken cancellationToken);
    Task<AssetAssignment?> GetByIdAsync(
        int assignmentId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<AssetAssignment>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<AssetProduct>> GetActiveProductsAsync(
        CancellationToken cancellationToken);
    Task<Employee?> GetEmployeeAsync(int employeeId, CancellationToken cancellationToken);
    Task<AssetProduct?> GetProductAsync(int productId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AssetUnit>> GetAvailableUnitsAsync(
        int productId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<AssetAssignmentBatchData>> GetAvailableBatchesAsync(
        int productId,
        CancellationToken cancellationToken);
    Task<(AssetAssignment Assignment, bool Created)> CreateAsync(
        AssetAssignment assignment,
        bool individual,
        CancellationToken cancellationToken);
    Task<AssetAssignment> ReturnAsync(
        int assignmentId,
        ReturnAssetAssignmentRequest request,
        AssetCondition returnedCondition,
        string? actor,
        CancellationToken cancellationToken);
}
