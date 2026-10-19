using ITAssetManagement.Domain.Entities;

namespace ITAssetManagement.Application.Repositories;

public interface IAssetProductRepository
{
    Task<IReadOnlyList<AssetProduct>> GetAllAsync(CancellationToken cancellationToken);
    Task<AssetProduct?> GetByIdAsync(int productId, CancellationToken cancellationToken);
    Task<bool> ActiveCategoryExistsAsync(int categoryId, CancellationToken cancellationToken);
    Task<bool> DuplicateExistsAsync(
        int categoryId,
        string productName,
        string brand,
        string? model,
        int? excludingProductId,
        CancellationToken cancellationToken);
    Task<bool> HasStockOrAssignmentHistoryAsync(int productId, CancellationToken cancellationToken);
    Task AddAsync(AssetProduct product, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
