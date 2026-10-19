using ITAssetManagement.Domain.Entities;

namespace ITAssetManagement.Application.Repositories;

public interface IAssetCategoryRepository
{
    Task<IReadOnlyList<AssetCategory>> GetAllAsync(CancellationToken cancellationToken);
    Task<AssetCategory?> GetByIdAsync(int categoryId, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(
        string normalizedName,
        int? excludingCategoryId,
        CancellationToken cancellationToken);
    Task AddAsync(AssetCategory category, CancellationToken cancellationToken);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
