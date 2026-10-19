using ITAssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class AssetAssignmentBatchAllocationConfiguration
    : IEntityTypeConfiguration<AssetAssignmentBatchAllocation>
{
    public void Configure(EntityTypeBuilder<AssetAssignmentBatchAllocation> builder)
    {
        builder.ToTable("AssetAssignmentBatchAllocations", table =>
            table.HasCheckConstraint(
                "CK_AssetAssignmentBatchAllocations_Quantity",
                "[Quantity] > 0"));
        builder.HasKey(allocation => allocation.AllocationId);
        builder.HasIndex(allocation => allocation.AssignmentItemId);
        builder.HasIndex(allocation => allocation.ReceiptItemId);
        builder.HasOne(allocation => allocation.AssignmentItem)
            .WithMany(item => item.BatchAllocations)
            .HasForeignKey(allocation => allocation.AssignmentItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(allocation => allocation.ReceiptItem)
            .WithMany(item => item.AssignmentBatchAllocations)
            .HasForeignKey(allocation => allocation.ReceiptItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ConfigureAudit();
    }
}
