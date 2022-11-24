using System.Linq.Expressions;

namespace EventFlowLite.Abstractions.Repository;

public interface IRepository<TEntity, in TId>
    where TEntity : class, IDatabaseEntity<TId>
    where TId : IComparable
{
    IQueryable<TEntity> AsQueryable();

    Task SaveChangesAsync();

    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool track = false,
        params Expression<Func<TEntity, object>>[] lazySelectors);

    Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> predicate, bool track = false,
        params Expression<Func<TEntity, object>>[] lazySelectors);

    Task<List<TEntity>> QueryListAsync(Expression<Func<TEntity, bool>> predicate,
        params Expression<Func<TEntity, object>>[] lazySelectors);

    Task<List<TEntity>> QueryListAsync(IEnumerable<Expression<Func<TEntity, bool>>> predicates,
        params Expression<Func<TEntity, object>>[] lazySelectors);

    ValueTask<TEntity?> FindAsync(TId value, bool track = false);
    ValueTask<TEntity?> FindManyAsync(params TId[] values);
    ValueTask<TEntity?> FindAsync(TId value, CancellationToken cancellationToken, bool track = false);
    ValueTask<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
    
    Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate,
        params Expression<Func<TEntity, object>>[] lazySelectors);
}