using ITAssetManagement.Application.DTOs;
using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Application.Services;
using ITAssetManagement.Domain.Entities;

namespace ITAssetManagement.Tests;

public sealed class AssetCategoryServiceTests
{
    [Fact]
    public async Task CreateAsync_TrimsFieldsAndSetsAuthenticatedAuditUser()
    {
        var repository = new FakeAssetCategoryRepository();
        var service = new AssetCategoryServiceImpl(repository);

        var created = await service.CreateAsync(
            new SaveAssetCategoryRequest("  Computers  ", "  Laptops and desktops  ", true),
            "admin",
            CancellationToken.None);

        Assert.Equal("Computers", created.CategoryName);
        Assert.Equal("Laptops and desktops", created.Description);
        Assert.True(created.IsActive);
        Assert.Equal("admin", created.CreatedBy);
        Assert.Null(created.ModifiedBy);
        Assert.Single(repository.Categories);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateIgnoringCaseAndSurroundingWhitespace()
    {
        var repository = new FakeAssetCategoryRepository(
            new AssetCategory { CategoryId = 1, CategoryName = "  Computers " });
        var service = new AssetCategoryServiceImpl(repository);

        await Assert.ThrowsAsync<DuplicateAssetCategoryNameException>(() =>
            service.CreateAsync(
                new SaveAssetCategoryRequest("computers", null),
                "admin",
                CancellationToken.None));

        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFieldsAndModifiedAuditUser()
    {
        var original = new AssetCategory
        {
            CategoryId = 1,
            CategoryName = "Computers",
            IsActive = true,
            CreatedBy = "creator",
        };
        var repository = new FakeAssetCategoryRepository(original);
        var service = new AssetCategoryServiceImpl(repository);

        var updated = await service.UpdateAsync(
            1,
            new SaveAssetCategoryRequest("  Computer Equipment ", "Equipment", false),
            "editor",
            CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("Computer Equipment", updated.CategoryName);
        Assert.Equal("Equipment", updated.Description);
        Assert.False(updated.IsActive);
        Assert.Equal("creator", updated.CreatedBy);
        Assert.Equal("editor", updated.ModifiedBy);
        Assert.NotNull(updated.ModifiedOn);
    }

    [Fact]
    public async Task UpdateAsync_RejectsAnotherCategoryNameButAllowsItsOwnName()
    {
        var first = new AssetCategory { CategoryId = 1, CategoryName = "Computers" };
        var second = new AssetCategory { CategoryId = 2, CategoryName = "Monitors" };
        var repository = new FakeAssetCategoryRepository(first, second);
        var service = new AssetCategoryServiceImpl(repository);

        await Assert.ThrowsAsync<DuplicateAssetCategoryNameException>(() =>
            service.UpdateAsync(
                1,
                new SaveAssetCategoryRequest(" monitors ", null),
                "admin",
                CancellationToken.None));

        var unchangedName = await service.UpdateAsync(
            1,
            new SaveAssetCategoryRequest(" computers ", null),
            "admin",
            CancellationToken.None);

        Assert.Equal("computers", unchangedName?.CategoryName);
    }

    [Fact]
    public async Task SetStatusAsync_ChangesStatusAndAuditUser()
    {
        var category = new AssetCategory { CategoryId = 7, CategoryName = "Printers" };
        var repository = new FakeAssetCategoryRepository(category);
        var service = new AssetCategoryServiceImpl(repository);

        var result = await service.SetStatusAsync(7, false, "admin", CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.IsActive);
        Assert.Equal("admin", result.ModifiedBy);
        Assert.NotNull(result.ModifiedOn);
    }

    [Fact]
    public async Task CreateAsync_RejectsEmptyCategoryName()
    {
        var repository = new FakeAssetCategoryRepository();
        var service = new AssetCategoryServiceImpl(repository);

        await Assert.ThrowsAsync<InvalidAssetCategoryException>(() =>
            service.CreateAsync(
                new SaveAssetCategoryRequest("  ", null),
                "admin",
                CancellationToken.None));

        Assert.Equal(0, repository.SaveCount);
    }

    private sealed class FakeAssetCategoryRepository(params AssetCategory[] categories)
        : IAssetCategoryRepository
    {
        public List<AssetCategory> Categories { get; } = [.. categories];
        public int SaveCount { get; private set; }

        public Task<IReadOnlyList<AssetCategory>> GetAllAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AssetCategory>>(Categories);
        }

        public Task<AssetCategory?> GetByIdAsync(int categoryId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Categories.SingleOrDefault(category => category.CategoryId == categoryId));
        }

        public Task<bool> NameExistsAsync(
            string normalizedName,
            int? excludingCategoryId,
            CancellationToken cancellationToken)
        {
            var exists = Categories.Any(category =>
                category.CategoryId != excludingCategoryId
                && string.Equals(
                    category.CategoryName.Trim(),
                    normalizedName,
                    StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }

        public Task AddAsync(AssetCategory category, CancellationToken cancellationToken)
        {
            category.CategoryId = Categories.Count == 0 ? 1 : Categories.Max(item => item.CategoryId) + 1;
            Categories.Add(category);
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            foreach (var category in Categories.Where(category => category.ModifiedBy is not null))
            {
                category.ModifiedOn = DateTime.UtcNow;
            }

            return Task.FromResult(1);
        }
    }
}
