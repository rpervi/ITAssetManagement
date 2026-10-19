using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Domain.Entities;
using ITAssetManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITAssetManagement.Infrastructure.Repositories;

public sealed class AssetCategoryRepository(AssetManagementDbContext dbContext)
    : IAssetCategoryRepository
{
    public async Task<IReadOnlyList<AssetCategory>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.AssetCategories
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<AssetCategory?> GetByIdAsync(
        int categoryId,
        CancellationToken cancellationToken)
    {
        return dbContext.AssetCategories
            .FirstOrDefaultAsync(category => category.CategoryId == categoryId, cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        string normalizedName,
        int? excludingCategoryId,
        CancellationToken cancellationToken)
    {
        return dbContext.AssetCategories.AnyAsync(
            category =>
                category.CategoryName.Trim().ToUpper() == normalizedName
                && (!excludingCategoryId.HasValue || category.CategoryId != excludingCategoryId.Value),
            cancellationToken);
    }

    public async Task AddAsync(AssetCategory category, CancellationToken cancellationToken)
    {
        await dbContext.AssetCategories.AddAsync(category, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
