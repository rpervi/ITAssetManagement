using ITAssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", table =>
            table.HasCheckConstraint("CK_Users_FailedLoginCount", "[FailedLoginCount] >= 0"));
        builder.HasKey(user => user.UserId);
        builder.Property(user => user.Username).HasMaxLength(100).IsRequired();
        builder.Property(user => user.NormalizedUsername).HasMaxLength(100).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(254).IsRequired();
        builder.Property(user => user.NormalizedEmail).HasMaxLength(254).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(user => user.IsActive).HasDefaultValue(true);
        builder.Property(user => user.LastLoginOn).HasColumnType("datetime2");
        builder.Property(user => user.LockoutEndOn).HasColumnType("datetime2");
        builder.HasIndex(user => user.NormalizedUsername).IsUnique();
        builder.HasIndex(user => user.NormalizedEmail).IsUnique();
        builder.ConfigureAudit();
    }
}
