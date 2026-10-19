using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Application.Services;
using ITAssetManagement.Domain.Entities;

namespace ITAssetManagement.Tests;

public sealed class AssetProductServiceTests
{
    [Fact]
    public async Task CreateAsync_NormalizesFieldsAndUsesAuthenticatedActor()
    {
        var category = new AssetCategory { CategoryId = 1, CategoryName = "Hardware", IsActive = true };
        var repository = new FakeAssetProductRepository([category]);
        var service = new AssetProductServiceImpl(repository);

        var product = await service.CreateAsync(
            Request(productName: "  Notebook  ", brand: "  Acme ", model: "  X1  "),
            "it-admin",
            CancellationToken.None);

        Assert.Equal("Notebook", product.ProductName);
        Assert.Equal("Acme", product.Brand);
        Assert.Equal("X1", product.Model);
        Assert.Equal("Bulk", product.TrackingType);
        Assert.Equal("Hardware", product.CategoryName);
        Assert.Equal("it-admin", product.CreatedBy);
        Assert.Single(repository.Products);
    }

    [Fact]
    public async Task CreateAsync_RejectsNormalizedDuplicate()
    {
        var category = new AssetCategory { CategoryId = 1, CategoryName = "Hardware", IsActive = true };
        var original = Product(1, category, "Notebook", "Acme", "X1");
        var repository = new FakeAssetProductRepository([category], [original]);
        var service = new AssetProductServiceImpl(repository);

        await Assert.ThrowsAsync<DuplicateAssetProductException>(() =>
            service.CreateAsync(
                Request(productName: " notebook ", brand: " ACME ", model: " x1 "),
                "it-admin",
                CancellationToken.None));
        Assert.Equal(0, repository.SaveCount);
    }

    [Theory]
    [InlineData("  ", "Acme", "Bulk", 0)]
    [InlineData("Notebook", "  ", "Bulk", 0)]
    [InlineData("Notebook", "Acme", "Other", 0)]
    [InlineData("Notebook", "Acme", "Individual", -1)]
    public async Task CreateAsync_RejectsInvalidFields(
        string name,
        string brand,
        string trackingType,
        int minimumStock)
    {
        var category = new AssetCategory { CategoryId = 1, CategoryName = "Hardware", IsActive = true };
        var repository = new FakeAssetProductRepository([category]);
        var service = new AssetProductServiceImpl(repository);

        await Assert.ThrowsAsync<InvalidAssetProductException>(() =>
            service.CreateAsync(
                Request(productName: name, brand: brand, trackingType: trackingType, minimumStock: minimumStock),
                "it-admin",
                CancellationToken.None));
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsInactiveOrMissingCategory()
    {
        var inactive = new AssetCategory { CategoryId = 1, CategoryName = "Inactive", IsActive = false };
        var repository = new FakeAssetProductRepository([inactive]);
        var service = new AssetProductServiceImpl(repository);

        await Assert.ThrowsAsync<InvalidAssetProductException>(() =>
            service.CreateAsync(Request(categoryId: 1), "it-admin", CancellationToken.None));
        await Assert.ThrowsAsync<InvalidAssetProductException>(() =>
            service.CreateAsync(Request(categoryId: 999), "it-admin", CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_RejectsTrackingTypeChangeAfterHistory()
    {
        var category = new AssetCategory { CategoryId = 1, CategoryName = "Hardware", IsActive = true };
        var product = Product(1, category, "Notebook", "Acme", "X1");
        var repository = new FakeAssetProductRepository([category], [product])
        {
            HasHistory = true,
        };
        var service = new AssetProductServiceImpl(repository);

        await Assert.ThrowsAsync<AssetProductHistoryException>(() =>
            service.UpdateAsync(
                1,
                Request(trackingType: "Individual"),
                "it-admin",
                CancellationToken.None));

        Assert.Equal(TrackingType.Bulk, product.TrackingType);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task UpdateAsync_AllowsUnchangedTrackingTypeAndSetsModifiedActor()
    {
        var category = new AssetCategory { CategoryId = 1, CategoryName = "Hardware", IsActive = true };
        var product = Product(1, category, "Notebook", "Acme", "X1");
        var repository = new FakeAssetProductRepository([category], [product])
        {
            HasHistory = true,
        };
        var service = new AssetProductServiceImpl(repository);

        var updated = await service.UpdateAsync(
            1,
            Request(productName: "Updated Notebook"),
            "editor",
            CancellationToken.None);

        Assert.Equal("Updated Notebook", updated?.ProductName);
        Assert.Equal("editor", updated?.ModifiedBy);
        Assert.Equal(TrackingType.Bulk, product.TrackingType);
    }

    [Fact]
    public async Task SetStatusAsync_ChangesProductStatus()
    {
        var category = new AssetCategory { CategoryId = 1, CategoryName = "Hardware", IsActive = true };
        var product = Product(1, category, "Notebook", "Acme", "X1");
        var service = new AssetProductServiceImpl(
            new FakeAssetProductRepository([category], [product]));

        var updated = await service.SetStatusAsync(1, false, "editor", CancellationToken.None);

        Assert.NotNull(updated);
        Assert.False(updated.IsActive);
        Assert.Equal("editor", updated.ModifiedBy);
    }

    private static SaveAssetProductRequest Request(
        int categoryId = 1,
        string productName = "Notebook",
        string brand = "Acme",
        string? model = "X1",
        string trackingType = "Bulk",
        int minimumStock = 0,
        bool isActive = true)
    {
        return new SaveAssetProductRequest(
            categoryId, productName, brand, model, trackingType, minimumStock, isActive);
    }

    private static AssetProduct Product(
        int id,
        AssetCategory category,
        string name,
        string brand,
        string? model)
    {
        return new AssetProduct
        {
            ProductId = id,
            CategoryId = category.CategoryId,
            Category = category,
            ProductName = name,
            Brand = brand,
            Model = model,
            TrackingType = TrackingType.Bulk,
            IsActive = true,
        };
    }

    private sealed class FakeAssetProductRepository(
        List<AssetCategory> categories,
        List<AssetProduct>? products = null) : IAssetProductRepository
    {
        public List<AssetProduct> Products { get; } = products ?? [];
        public int SaveCount { get; private set; }
        public bool HasHistory { get; init; }

        public Task<IReadOnlyList<AssetProduct>> GetAllAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AssetProduct>>(Products);
        }

        public Task<AssetProduct?> GetByIdAsync(int productId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Products.SingleOrDefault(product => product.ProductId == productId));
        }

        public Task<bool> ActiveCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(categories.Any(
                category => category.CategoryId == categoryId && category.IsActive));
        }

        public Task<bool> DuplicateExistsAsync(
            int categoryId,
            string productName,
            string brand,
            string? model,
            int? excludingProductId,
            CancellationToken cancellationToken)
        {
            var exists = Products.Any(product =>
                product.CategoryId == categoryId
                && product.ProductId != excludingProductId
                && string.Equals(product.ProductName.Trim(), productName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(product.Brand?.Trim(), brand, StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    string.IsNullOrWhiteSpace(product.Model) ? null : product.Model.Trim(),
                    model,
                    StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }

        public Task<bool> HasStockOrAssignmentHistoryAsync(
            int productId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(HasHistory);
        }

        public Task AddAsync(AssetProduct product, CancellationToken cancellationToken)
        {
            product.ProductId = Products.Count == 0 ? 1 : Products.Max(item => item.ProductId) + 1;
            product.Category = categories.Single(category => category.CategoryId == product.CategoryId);
            Products.Add(product);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
