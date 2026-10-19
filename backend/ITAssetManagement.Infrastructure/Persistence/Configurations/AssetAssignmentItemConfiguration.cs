using ITAssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class AssetAssignmentItemConfiguration : IEntityTypeConfiguration<AssetAssignmentItem>
{
    public void Configure(EntityTypeBuilder<AssetAssignmentItem> builder)
    {
        builder.ToTable("AssetAssignmentItems", table =>
        {
            table.HasCheckConstraint("CK_AssetAssignmentItems_Quantity", "[Quantity] > 0");
            table.HasCheckConstraint(
                "CK_AssetAssignmentItems_IndividualQuantity",
                "[AssetUnitId] IS NULL OR [Quantity] = 1");
        });
        builder.HasKey(item => item.AssignmentItemId);
        builder.HasIndex(item => item.AssignmentId);
        builder.HasIndex(item => item.ProductId);
        builder.HasIndex(item => item.AssetUnitId);
        builder.HasOne(item => item.Assignment)
            .WithMany(assignment => assignment.Items)
            .HasForeignKey(item => item.AssignmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Product)
            .WithMany(product => product.AssetAssignmentItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.AssetUnit)
            .WithMany(unit => unit.AssignmentItems)
            .HasForeignKey(item => item.AssetUnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ConfigureAudit();
    }
}
