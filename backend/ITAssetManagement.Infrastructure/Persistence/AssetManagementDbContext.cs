using ITAssetManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ITAssetManagement.Infrastructure.Persistence;

public sealed class AssetManagementDbContext(DbContextOptions<AssetManagementDbContext> options)
    : DbContext(options)
{
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<AssetProduct> AssetProducts => Set<AssetProduct>();
    public DbSet<AssetUnit> AssetUnits => Set<AssetUnit>();
    public DbSet<StockReceipt> StockReceipts => Set<StockReceipt>();
    public DbSet<StockReceiptItem> StockReceiptItems => Set<StockReceiptItem>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AssetAssignment> AssetAssignments => Set<AssetAssignment>();
    public DbSet<AssetAssignmentItem> AssetAssignmentItems => Set<AssetAssignmentItem>();
    public DbSet<AssetAssignmentBatchAllocation> AssetAssignmentBatchAllocations =>
        Set<AssetAssignmentBatchAllocation>();
    public DbSet<AssetReturn> AssetReturns => Set<AssetReturn>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssetManagementDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedOn = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(entity => entity.CreatedOn).IsModified = false;
                entry.Entity.ModifiedOn = now;
            }
        }
    }
}
