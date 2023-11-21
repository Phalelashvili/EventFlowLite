using System.Linq.Expressions;

namespace EventFlowLite.Abstractions.Repository;

public interface IRepository<TEntity, in TId>
    where TEntity : class, IDatabaseEntity<TId>
    where TId : IComparable
{
    IQueryable<TEntity> AsQueryable();

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken,
        bool track = false, params Expression<Func<TEntity, object>>[] lazySelectors);

    Task<TEntity?> LastOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken,
        bool track = false, params Expression<Func<TEntity, object>>[] lazySelectors);

    Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken,
        bool track = false, params Expression<Func<TEntity, object>>[] lazySelectors);

    Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken, 
        bool track = false, params Expression<Func<TEntity, object>>[] lazySelectors);

    Task<List<TEntity>> QueryListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken,
        params Expression<Func<TEntity, object>>[] lazySelectors);

    Task<List<TEntity>> QueryListAsync(IEnumerable<Expression<Func<TEntity, bool>>> predicates,
        CancellationToken cancellationToken,
        params Expression<Func<TEntity, object>>[] lazySelectors);

    ValueTask<TEntity?> FindAsync(TId value, CancellationToken cancellationToken, bool track = false);
    ValueTask<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken);
    
    Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken,
        params Expression<Func<TEntity, object>>[] lazySelectors);

    void Remove(TEntity entity);
}