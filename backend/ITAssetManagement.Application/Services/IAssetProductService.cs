using ITAssetManagement.Application.DTOs;

namespace ITAssetManagement.Application.Services;

public interface IAssetProductService
{
    Task<IReadOnlyList<AssetProductDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<AssetProductDto?> GetByIdAsync(int productId, CancellationToken cancellationToken);
    Task<AssetProductDto> CreateAsync(
        SaveAssetProductRequest request,
        string? actor,
        CancellationToken cancellationToken);
    Task<AssetProductDto?> UpdateAsync(
        int productId,
        SaveAssetProductRequest request,
        string? actor,
        CancellationToken cancellationToken);
    Task<AssetProductDto?> SetStatusAsync(
        int productId,
        bool isActive,
        string? actor,
        CancellationToken cancellationToken);
}
