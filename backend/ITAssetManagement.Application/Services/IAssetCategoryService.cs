using ITAssetManagement.Application.DTOs;

namespace ITAssetManagement.Application.Services;

public interface IAssetCategoryService
{
    Task<IReadOnlyList<AssetCategoryDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<AssetCategoryDto?> GetByIdAsync(int categoryId, CancellationToken cancellationToken);
    Task<AssetCategoryDto> CreateAsync(
        SaveAssetCategoryRequest request,
        string? actor,
        CancellationToken cancellationToken);
    Task<AssetCategoryDto?> UpdateAsync(
        int categoryId,
        SaveAssetCategoryRequest request,
        string? actor,
        CancellationToken cancellationToken);
    Task<AssetCategoryDto?> SetStatusAsync(
        int categoryId,
        bool isActive,
        string? actor,
        CancellationToken cancellationToken);
}
