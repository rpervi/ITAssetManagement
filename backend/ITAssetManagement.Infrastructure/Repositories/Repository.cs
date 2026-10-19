using ITAssetManagement.Application.Repositories;
using ITAssetManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ITAssetManagement.Infrastructure.Repositories;

public sealed class Repository<TEntity>(AssetManagementDbContext dbContext) : IRepository<TEntity>
    where TEntity : class
{
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TEntity>().ToListAsync(cancellationToken);
    }

    public async Task<TEntity?> GetByIdAsync(
        object id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Set<TEntity>().FindAsync([id], cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        dbContext.Set<TEntity>().Update(entity);
    }

    public void Delete(TEntity entity)
    {
        dbContext.Set<TEntity>().Remove(entity);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
