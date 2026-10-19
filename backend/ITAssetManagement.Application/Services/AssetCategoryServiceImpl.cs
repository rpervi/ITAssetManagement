using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Domain.Entities;

namespace ITAssetManagement.Application.Services;

public sealed class AssetCategoryServiceImpl(IAssetCategoryRepository repository)
    : IAssetCategoryService
{
    public async Task<IReadOnlyList<AssetCategoryDto>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var categories = await repository.GetAllAsync(cancellationToken);
        return categories
            .OrderByDescending(category => category.CreatedOn)
            .ThenBy(category => category.CategoryName)
            .Select(ToDto)
            .ToArray();
    }

    public async Task<AssetCategoryDto?> GetByIdAsync(
        int categoryId,
        CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(categoryId, cancellationToken);
        return category is null ? null : ToDto(category);
    }

    public async Task<AssetCategoryDto> CreateAsync(
        SaveAssetCategoryRequest request,
        string? actor,
        CancellationToken cancellationToken)
    {
        var categoryName = NormalizeName(request.CategoryName);
        await EnsureNameIsAvailableAsync(categoryName, null, cancellationToken);

        var category = new AssetCategory
        {
            CategoryName = categoryName,
            Description = NormalizeDescription(request.Description),
            IsActive = request.IsActive,
            CreatedBy = NormalizeActor(actor),
        };

        await repository.AddAsync(category, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(category);
    }

    public async Task<AssetCategoryDto?> UpdateAsync(
        int categoryId,
        SaveAssetCategoryRequest request,
        string? actor,
        CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
        {
            return null;
        }

        var categoryName = NormalizeName(request.CategoryName);
        await EnsureNameIsAvailableAsync(categoryName, categoryId, cancellationToken);

        category.CategoryName = categoryName;
        category.Description = NormalizeDescription(request.Description);
        category.IsActive = request.IsActive;
        category.ModifiedBy = NormalizeActor(actor);
        await repository.SaveChangesAsync(cancellationToken);

        return ToDto(category);
    }

    public async Task<AssetCategoryDto?> SetStatusAsync(
        int categoryId,
        bool isActive,
        string? actor,
        CancellationToken cancellationToken)
    {
        var category = await repository.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
        {
            return null;
        }

        category.IsActive = isActive;
        category.ModifiedBy = NormalizeActor(actor);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(category);
    }

    private async Task EnsureNameIsAvailableAsync(
        string name,
        int? excludingCategoryId,
        CancellationToken cancellationToken)
    {
        if (await repository.NameExistsAsync(name.ToUpperInvariant(), excludingCategoryId, cancellationToken))
        {
            throw new DuplicateAssetCategoryNameException(
                "A category with this name already exists.");
        }
    }

    private static string NormalizeName(string? categoryName)
    {
        var normalized = categoryName?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidAssetCategoryException("Category name is required.");
        }

        if (normalized.Length > 100)
        {
            throw new InvalidAssetCategoryException("Category name cannot exceed 100 characters.");
        }

        return normalized;
    }

    private static string? NormalizeDescription(string? description)
    {
        var normalized = description?.Trim();
        if (normalized is { Length: > 500 })
        {
            throw new InvalidAssetCategoryException("Description cannot exceed 500 characters.");
        }

        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static string? NormalizeActor(string? actor)
    {
        var normalized = actor?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized[..Math.Min(normalized.Length, 100)];
    }

    private static AssetCategoryDto ToDto(AssetCategory category)
    {
        return new AssetCategoryDto(
            category.CategoryId,
            category.CategoryName,
            category.Description,
            category.IsActive,
            category.CreatedBy,
            category.CreatedOn,
            category.ModifiedBy,
            category.ModifiedOn);
    }
}
