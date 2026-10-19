using ITAssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class AssetReturnConfiguration : IEntityTypeConfiguration<AssetReturn>
{
    public void Configure(EntityTypeBuilder<AssetReturn> builder)
    {
        builder.ToTable("AssetReturns", table =>
            table.HasCheckConstraint("CK_AssetReturns_ReturnQuantity", "[ReturnQuantity] > 0"));
        builder.HasKey(assetReturn => assetReturn.ReturnId);
        builder.Property(assetReturn => assetReturn.ReturnDate).HasColumnType("datetime2");
        builder.Property(assetReturn => assetReturn.ReturnedCondition)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(assetReturn => assetReturn.Remarks).HasMaxLength(1000);
        builder.HasIndex(assetReturn => assetReturn.AssignmentItemId);
        builder.HasOne(assetReturn => assetReturn.AssignmentItem)
            .WithMany(item => item.Returns)
            .HasForeignKey(assetReturn => assetReturn.AssignmentItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ConfigureAudit();
    }
}
