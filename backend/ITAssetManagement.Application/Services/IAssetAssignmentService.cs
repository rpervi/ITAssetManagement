using ITAssetManagement.Application.DTOs;

namespace ITAssetManagement.Application.Services;

public interface IAssetAssignmentService
{
    Task<AssetAssignmentPageResponse> GetPageAsync(
        string? search,
        int? employeeId,
        int? productId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken);
    Task<AssetAssignmentResponse?> GetByIdAsync(
        int assignmentId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<AssetAssignmentResponse>> GetByEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<AssetAssignmentProductDto>> GetActiveProductsAsync(
        CancellationToken cancellationToken);
    Task<IReadOnlyList<AvailableAssetUnitResponse>?> GetAvailableUnitsAsync(
        int productId,
        CancellationToken cancellationToken);
    Task<AvailableStockResponse?> GetAvailableStockAsync(
        int productId,
        CancellationToken cancellationToken);
    Task<AssetAssignmentSaveResult> CreateAsync(
        CreateAssetAssignmentRequest request,
        string? actor,
        CancellationToken cancellationToken);
}
