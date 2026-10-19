using ITAssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ITAssetManagement.Infrastructure.Persistence.Configurations;

internal static class AuditableEntityConfigurationExtensions
{
    internal static void ConfigureAudit<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(100);
        builder.Property(entity => entity.CreatedOn)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(entity => entity.ModifiedBy).HasMaxLength(100);
        builder.Property(entity => entity.ModifiedOn).HasColumnType("datetime2");
    }
}
