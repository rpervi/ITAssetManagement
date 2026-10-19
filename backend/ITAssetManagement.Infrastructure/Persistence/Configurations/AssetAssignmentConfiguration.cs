using ITAssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class AssetAssignmentConfiguration : IEntityTypeConfiguration<AssetAssignment>
{
    public void Configure(EntityTypeBuilder<AssetAssignment> builder)
    {
        builder.ToTable("AssetAssignments");
        builder.HasKey(assignment => assignment.AssignmentId);
        builder.HasIndex(assignment => assignment.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");
        builder.Property(assignment => assignment.AssignedDate).HasColumnType("datetime2");
        builder.Property(assignment => assignment.AssignedBy).HasMaxLength(100).IsRequired();
        builder.Property(assignment => assignment.Remarks).HasMaxLength(1000);
        builder.HasIndex(assignment => assignment.EmployeeId);
        builder.HasIndex(assignment => assignment.AssignedDate);
        builder.HasOne(assignment => assignment.Employee)
            .WithMany(employee => employee.AssetAssignments)
            .HasForeignKey(assignment => assignment.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ConfigureAudit();
    }
}
