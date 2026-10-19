using ITAssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("StockAdjustments", table =>
            table.HasCheckConstraint("CK_StockAdjustments_QuantityChange", "[QuantityChange] <> 0"));
        builder.HasKey(adjustment => adjustment.AdjustmentId);
        builder.Property(adjustment => adjustment.Reason).HasMaxLength(500).IsRequired();
        builder.Property(adjustment => adjustment.AdjustmentDate).HasColumnType("datetime2");
        builder.Property(adjustment => adjustment.ApprovedBy).HasMaxLength(100).IsRequired();
        builder.HasIndex(adjustment => adjustment.ProductId);
        builder.HasIndex(adjustment => adjustment.AdjustmentDate);
        builder.HasOne(adjustment => adjustment.Product)
            .WithMany(product => product.StockAdjustments)
            .HasForeignKey(adjustment => adjustment.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ConfigureAudit();
    }
}
