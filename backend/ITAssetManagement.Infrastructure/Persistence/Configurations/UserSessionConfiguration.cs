using ITAssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAssetManagement.Infrastructure.Persistence.Configurations;

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");
        builder.HasKey(session => session.SessionId);
        builder.Property(session => session.RefreshTokenHash).HasMaxLength(64).IsRequired();
        builder.Property(session => session.ExpiresOn).HasColumnType("datetime2");
        builder.Property(session => session.RevokedOn).HasColumnType("datetime2");
        builder.HasIndex(session => session.RefreshTokenHash).IsUnique();
        builder.HasIndex(session => new { session.UserId, session.ExpiresOn });
        builder.HasOne(session => session.User)
            .WithMany(user => user.UserSessions)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.ConfigureAudit();
    }
}
