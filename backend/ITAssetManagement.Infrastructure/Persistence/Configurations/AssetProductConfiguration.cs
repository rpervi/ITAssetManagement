using ITAssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class AssetProductConfiguration : IEntityTypeConfiguration<AssetProduct>
{
    public void Configure(EntityTypeBuilder<AssetProduct> builder)
    {
        builder.ToTable("AssetProducts", table =>
        {
            table.HasCheckConstraint("CK_AssetProducts_MinimumStock", "[MinimumStock] >= 0");
            table.HasCheckConstraint(
                "CK_AssetProducts_TrackingType",
                "[TrackingType] IN ('Individual', 'Bulk')");
        });
        builder.HasKey(product => product.ProductId);
        builder.Property(product => product.ProductName).HasMaxLength(150).IsRequired();
        builder.Property(product => product.Brand).HasMaxLength(100);
        builder.Property(product => product.Model).HasMaxLength(100);
        builder.Property(product => product.TrackingType).HasConversion<string>().HasMaxLength(20);
        builder.Property(product => product.IsActive).HasDefaultValue(true);
        builder.HasIndex(product => product.CategoryId);
        builder.HasOne(product => product.Category)
            .WithMany(category => category.AssetProducts)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ConfigureAudit();
    }
}
