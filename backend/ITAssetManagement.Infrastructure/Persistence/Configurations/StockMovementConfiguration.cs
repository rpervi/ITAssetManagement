using ITAssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements", table =>
            table.HasCheckConstraint("CK_StockMovements_Quantity", "[Quantity] > 0"));
        builder.HasKey(movement => movement.MovementId);
        builder.Property(movement => movement.MovementType).HasConversion<string>().HasMaxLength(30);
        builder.Property(movement => movement.MovementDate).HasColumnType("datetime2");
        builder.Property(movement => movement.Remarks).HasMaxLength(1000);
        builder.HasIndex(movement => movement.ProductId);
        builder.HasIndex(movement => movement.ReceiptItemId);
        builder.HasIndex(movement => movement.AssetUnitId);
        builder.HasIndex(movement => movement.MovementDate);
        builder.HasIndex(movement => new { movement.MovementType, movement.ReferenceId });
        builder.HasOne(movement => movement.Product)
            .WithMany(product => product.StockMovements)
            .HasForeignKey(movement => movement.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(movement => movement.ReceiptItem)
            .WithMany(item => item.StockMovements)
            .HasForeignKey(movement => movement.ReceiptItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(movement => movement.AssetUnit)
            .WithMany(unit => unit.StockMovements)
            .HasForeignKey(movement => movement.AssetUnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ConfigureAudit();
    }
}
