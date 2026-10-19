using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Domain.Entities;

namespace ITAssetManagement.Application.Services;

public sealed class AssetProductServiceImpl(IAssetProductRepository repository)
    : IAssetProductService
{
    public async Task<IReadOnlyList<AssetProductDto>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var products = await repository.GetAllAsync(cancellationToken);
        return products
            .OrderBy(product => product.Category?.CategoryName)
            .ThenBy(product => product.ProductName)
            .Select(ToDto)
            .ToArray();
    }

    public async Task<AssetProductDto?> GetByIdAsync(
        int productId,
        CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(productId, cancellationToken);
        return product is null ? null : ToDto(product);
    }

    public async Task<AssetProductDto> CreateAsync(
        SaveAssetProductRequest request,
        string? actor,
        CancellationToken cancellationToken)
    {
        var values = ValidateAndNormalize(request);
        await EnsureCategoryIsActiveAsync(values.CategoryId, cancellationToken);
        await EnsureUniqueAsync(values, null, cancellationToken);

        var product = new AssetProduct
        {
            CategoryId = values.CategoryId,
            ProductName = values.ProductName,
            Brand = values.Brand,
            Model = values.Model,
            TrackingType = values.TrackingType,
            MinimumStock = values.MinimumStock,
            IsActive = request.IsActive,
            CreatedBy = NormalizeActor(actor),
        };

        await repository.AddAsync(product, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(product);
    }

    public async Task<AssetProductDto?> UpdateAsync(
        int productId,
        SaveAssetProductRequest request,
        string? actor,
        CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var values = ValidateAndNormalize(request);
        await EnsureCategoryIsActiveAsync(values.CategoryId, cancellationToken);
        await EnsureUniqueAsync(values, productId, cancellationToken);

        if (product.TrackingType != values.TrackingType
            && await repository.HasStockOrAssignmentHistoryAsync(productId, cancellationToken))
        {
            throw new AssetProductHistoryException(
                "Tracking type cannot be changed after stock or assignment history exists.");
        }

        product.CategoryId = values.CategoryId;
        product.ProductName = values.ProductName;
        product.Brand = values.Brand;
        product.Model = values.Model;
        product.TrackingType = values.TrackingType;
        product.MinimumStock = values.MinimumStock;
        product.IsActive = request.IsActive;
        product.ModifiedBy = NormalizeActor(actor);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(product);
    }

    public async Task<AssetProductDto?> SetStatusAsync(
        int productId,
        bool isActive,
        string? actor,
        CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(productId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        product.IsActive = isActive;
        product.ModifiedBy = NormalizeActor(actor);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(product);
    }

    private async Task EnsureCategoryIsActiveAsync(
        int categoryId,
        CancellationToken cancellationToken)
    {
        if (!await repository.ActiveCategoryExistsAsync(categoryId, cancellationToken))
        {
            throw new InvalidAssetProductException(
                "Select an existing active asset category.");
        }
    }

    private async Task EnsureUniqueAsync(
        NormalizedProduct values,
        int? excludingProductId,
        CancellationToken cancellationToken)
    {
        if (await repository.DuplicateExistsAsync(
                values.CategoryId,
                values.ProductName,
                values.Brand,
                values.Model,
                excludingProductId,
                cancellationToken))
        {
            throw new DuplicateAssetProductException(
                "A product with this category, name, brand, and model already exists.");
        }
    }

    private static NormalizedProduct ValidateAndNormalize(SaveAssetProductRequest request)
    {
        var productName = NormalizeRequired(request.ProductName, 150, "Product name");
        var brand = NormalizeRequired(request.Brand, 100, "Brand");
        var model = NormalizeOptional(request.Model, 100, "Model");
        if (request.CategoryId <= 0)
        {
            throw new InvalidAssetProductException("An asset category is required.");
        }

        if (request.MinimumStock < 0)
        {
            throw new InvalidAssetProductException("Minimum stock cannot be negative.");
        }

        var trackingValue = request.TrackingType?.Trim();
        if (string.Equals(trackingValue, nameof(TrackingType.Individual), StringComparison.OrdinalIgnoreCase))
        {
            return new NormalizedProduct(
                request.CategoryId, productName, brand, model, TrackingType.Individual, request.MinimumStock);
        }

        if (string.Equals(trackingValue, nameof(TrackingType.Bulk), StringComparison.OrdinalIgnoreCase))
        {
            return new NormalizedProduct(
                request.CategoryId, productName, brand, model, TrackingType.Bulk, request.MinimumStock);
        }

        throw new InvalidAssetProductException("Tracking type must be Individual or Bulk.");
    }

    private static string NormalizeRequired(string? value, int maxLength, string fieldName)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidAssetProductException($"{fieldName} is required.");
        }

        if (normalized.Length > maxLength)
        {
            throw new InvalidAssetProductException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string fieldName)
    {
        var normalized = value?.Trim();
        if (normalized is { Length: > 0 } && normalized.Length > maxLength)
        {
            throw new InvalidAssetProductException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static string? NormalizeActor(string? actor)
    {
        var normalized = actor?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized[..Math.Min(normalized.Length, 100)];
    }

    private static AssetProductDto ToDto(AssetProduct product)
    {
        return new AssetProductDto(
            product.ProductId,
            product.CategoryId,
            product.Category?.CategoryName ?? string.Empty,
            product.ProductName,
            product.Brand ?? string.Empty,
            product.Model,
            product.TrackingType.ToString(),
            product.MinimumStock,
            product.IsActive,
            product.CreatedBy,
            product.CreatedOn,
            product.ModifiedBy,
            product.ModifiedOn);
    }

    private sealed record NormalizedProduct(
        int CategoryId,
        string ProductName,
        string Brand,
        string? Model,
        TrackingType TrackingType,
        int MinimumStock);
}
