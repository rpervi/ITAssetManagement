namespace ITAssetManagement.Application.Repositories;

public interface IRepository<TEntity>
    where TEntity : class
{
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Delete(TEntity entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
