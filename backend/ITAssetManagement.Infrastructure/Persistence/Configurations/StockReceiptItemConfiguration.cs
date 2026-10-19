using ITAssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class StockReceiptItemConfiguration : IEntityTypeConfiguration<StockReceiptItem>
{
    public void Configure(EntityTypeBuilder<StockReceiptItem> builder)
    {
        builder.ToTable("StockReceiptItems", table =>
        {
            table.HasCheckConstraint("CK_StockReceiptItems_Quantity", "[Quantity] > 0");
            table.HasCheckConstraint("CK_StockReceiptItems_UnitPrice", "[UnitPrice] >= 0");
            table.HasCheckConstraint(
                "CK_StockReceiptItems_Condition",
                "[Condition] IN ('New', 'Good', 'Fair', 'Poor', 'Damaged')");
        });
        builder.HasKey(item => item.ReceiptItemId);
        builder.Property(item => item.UnitPrice).HasPrecision(18, 2);
        builder.Property(item => item.PurchaseDate).HasColumnType("datetime2");
        builder.Property(item => item.WarrantyExpiryDate).HasColumnType("datetime2");
        builder.Property(item => item.Condition)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(AssetCondition.Good)
            .HasSentinel((AssetCondition)0);
        builder.HasIndex(item => item.ReceiptId);
        builder.HasIndex(item => item.ProductId);
        builder.HasOne(item => item.Receipt)
            .WithMany(receipt => receipt.Items)
            .HasForeignKey(item => item.ReceiptId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Product)
            .WithMany(product => product.StockReceiptItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ConfigureAudit();
    }
}
