using ITAssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(role => role.RoleId);
        builder.Property(role => role.RoleName).HasMaxLength(50).IsRequired();
        builder.Property(role => role.IsActive).HasDefaultValue(true);
        builder.HasIndex(role => role.RoleName).IsUnique();
        builder.HasData(
            new Role { RoleId = 1, RoleName = "Admin", IsActive = true },
            new Role { RoleId = 2, RoleName = "ITStaff", IsActive = true },
            new Role { RoleId = 3, RoleName = "Viewer", IsActive = true });
    }
}
