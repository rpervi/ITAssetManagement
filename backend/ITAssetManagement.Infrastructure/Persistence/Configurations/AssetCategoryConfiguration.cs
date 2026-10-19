using ITAssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class AssetCategoryConfiguration : IEntityTypeConfiguration<AssetCategory>
{
    public void Configure(EntityTypeBuilder<AssetCategory> builder)
    {
        builder.ToTable("AssetCategories");
        builder.HasKey(category => category.CategoryId);
        builder.Property(category => category.CategoryName).HasMaxLength(100).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(500);
        builder.Property(category => category.IsActive).HasDefaultValue(true);
        builder.HasIndex(category => category.CategoryName).IsUnique();
        builder.ConfigureAudit();
    }
}
