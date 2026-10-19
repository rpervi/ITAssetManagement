using ITAssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class AssetUnitConfiguration : IEntityTypeConfiguration<AssetUnit>
{
    public void Configure(EntityTypeBuilder<AssetUnit> builder)
    {
        builder.ToTable("AssetUnits", table =>
        {
            table.HasCheckConstraint("CK_AssetUnits_PurchaseCost", "[PurchaseCost] >= 0");
            table.HasCheckConstraint(
                "CK_AssetUnits_Status",
                "[Status] IN ('Available', 'Assigned', 'UnderRepair', 'Retired')");
            table.HasCheckConstraint(
                "CK_AssetUnits_Condition",
                "[Condition] IN ('New', 'Good', 'Fair', 'Poor', 'Damaged')");
        });
        builder.HasKey(unit => unit.AssetUnitId);
        builder.Property(unit => unit.AssetCode).HasMaxLength(100).IsRequired();
        builder.Property(unit => unit.SerialNumber).HasMaxLength(100);
        builder.Property(unit => unit.PurchaseDate).HasColumnType("datetime2");
        builder.Property(unit => unit.WarrantyExpiryDate).HasColumnType("datetime2");
        builder.Property(unit => unit.PurchaseCost).HasPrecision(18, 2);
        builder.Property(unit => unit.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(unit => unit.Condition).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(unit => unit.AssetCode).IsUnique();
        builder.HasIndex(unit => unit.SerialNumber)
            .IsUnique()
            .HasFilter("[SerialNumber] IS NOT NULL AND [SerialNumber] <> N''");
        builder.HasIndex(unit => unit.ProductId);
        builder.HasOne(unit => unit.Product)
            .WithMany(product => product.AssetUnits)
            .HasForeignKey(unit => unit.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ConfigureAudit();
    }
}
