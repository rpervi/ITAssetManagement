using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Domain.Entities;
using ITAssetManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITAssetManagement.Infrastructure.Repositories;

public sealed class AssetProductRepository(AssetManagementDbContext dbContext)
    : IAssetProductRepository
{
    public async Task<IReadOnlyList<AssetProduct>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.AssetProducts
            .AsNoTracking()
            .Include(product => product.Category)
            .ToListAsync(cancellationToken);
    }

    public Task<AssetProduct?> GetByIdAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        return dbContext.AssetProducts
            .Include(product => product.Category)
            .FirstOrDefaultAsync(product => product.ProductId == productId, cancellationToken);
    }

    public Task<bool> ActiveCategoryExistsAsync(
        int categoryId,
        CancellationToken cancellationToken)
    {
        return dbContext.AssetCategories.AnyAsync(
            category => category.CategoryId == categoryId && category.IsActive,
            cancellationToken);
    }

    public Task<bool> DuplicateExistsAsync(
        int categoryId,
        string productName,
        string brand,
        string? model,
        int? excludingProductId,
        CancellationToken cancellationToken)
    {
        var normalizedName = productName.Trim().ToUpper();
        var normalizedBrand = brand.Trim().ToUpper();
        var normalizedModel = model == null ? null : model.Trim().ToUpper();

        return dbContext.AssetProducts.AnyAsync(
            product =>
                product.CategoryId == categoryId
                && product.ProductName.Trim().ToUpper() == normalizedName
                && product.Brand != null
                && product.Brand.Trim().ToUpper() == normalizedBrand
                && (product.Model == null || product.Model.Trim() == ""
                    ? null
                    : product.Model.Trim().ToUpper()) == normalizedModel
                && (!excludingProductId.HasValue || product.ProductId != excludingProductId.Value),
            cancellationToken);
    }

    public async Task<bool> HasStockOrAssignmentHistoryAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AssetUnits.AnyAsync(
                asset => asset.ProductId == productId, cancellationToken)
            || await dbContext.StockReceiptItems.AnyAsync(
                item => item.ProductId == productId, cancellationToken)
            || await dbContext.AssetAssignmentItems.AnyAsync(
                item => item.ProductId == productId, cancellationToken)
            || await dbContext.StockMovements.AnyAsync(
                movement => movement.ProductId == productId, cancellationToken)
            || await dbContext.StockAdjustments.AnyAsync(
                adjustment => adjustment.ProductId == productId, cancellationToken);
    }

    public async Task AddAsync(AssetProduct product, CancellationToken cancellationToken)
    {
        await dbContext.AssetProducts.AddAsync(product, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
