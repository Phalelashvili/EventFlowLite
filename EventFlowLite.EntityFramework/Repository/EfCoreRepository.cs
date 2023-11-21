using System.Linq.Expressions;
using EventFlowLite.Abstractions;
using EventFlowLite.Abstractions.Repository;
using EventFlowLite.EntityFramework.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EventFlowLite.EntityFramework.Repository;

public class EfCoreRepository<TContext, TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : DatabaseEntity<TId>
    where TId : IComparable
    where TContext : DbContext
{
    protected readonly TContext DbContext;
    protected readonly DbSet<TEntity> DbSet;

    public EfCoreRepository(TContext dbContext)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>();
    }

    public IQueryable<TEntity> AsQueryable()
    {
        return DbSet.AsQueryable();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        var entries = DbContext.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified ||
                        e.References.Any(r =>
                            r.TargetEntry != null && r.TargetEntry.Metadata.IsOwned() &&
                            r.TargetEntry.State is EntityState.Modified));

        var now = DateTime.UtcNow;
        foreach (var entityEntry in entries)
        {
            var entity = entityEntry.Entity as TEntity;
            if (entity is null)
                continue;
            entity.ConcurrencyVersion++;
            entity.DateTimeUpdated = now;

            if (entityEntry.State == EntityState.Added)
                entity.DateTimeCreated = now;
        }

        return DbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken, bool track = false,
        params Expression<Func<TEntity, object>>[] lazySelectors)
    {
        return DbSet
            .WithLazySelectors<TEntity, TId>(lazySelectors)
            .WithTracking<TEntity, TId>(track)
            .FirstOrDefaultAsync(predicate, cancellationToken: cancellationToken);
    }
    public Task<TEntity?> LastOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken, bool track = false,
        params Expression<Func<TEntity, object>>[] lazySelectors)
    {
        return DbSet
            .WithLazySelectors<TEntity, TId>(lazySelectors)
            .WithTracking<TEntity, TId>(track)
            .OrderByDescending(entity => entity.Id)
            .LastOrDefaultAsync(predicate, cancellationToken: cancellationToken);
    }

    public Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken, bool track = false,
        params Expression<Func<TEntity, object>>[] lazySelectors)
    {
        return DbSet
            .WithLazySelectors<TEntity, TId>(lazySelectors)
            .WithTracking<TEntity, TId>(track)
            .SingleOrDefaultAsync(predicate, cancellationToken: cancellationToken);
    }

    public async Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken, bool track = false,
        params Expression<Func<TEntity, object>>[] lazySelectors)
    {
        var entity = await FirstOrDefaultAsync(predicate, cancellationToken, track, lazySelectors);
        if (entity == default)
            throw new ApplicationException($"no matching {typeof(TEntity).Name} found where {predicate}");
        return entity;
    }

    public Task<List<TEntity>> QueryListAsync(Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken, params Expression<Func<TEntity, object>>[] lazySelectors)
    {
        return DbSet
            .WithLazySelectors<TEntity, TId>(lazySelectors)
            .Where(predicate)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public Task<List<TEntity>> QueryListAsync(IEnumerable<Expression<Func<TEntity, bool>>> predicates, 
        CancellationToken cancellationToken,
        params Expression<Func<TEntity, object>>[] lazySelectors)
    {
        var q = DbSet.WithLazySelectors<TEntity, TId>(lazySelectors).AsQueryable();

        foreach (var predicate in predicates)
            q = q.Where(predicate);

        return q.ToListAsync(cancellationToken: cancellationToken);
    }
    
    public ValueTask<TEntity?> FindAsync(TId value, CancellationToken cancellationToken, bool track = false)
    {
        return DbSet.WithTracking<TEntity, TId>(track).FindAsync(new object[]{value}, cancellationToken);
    }

    public async ValueTask<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken)
    {
        var result = await DbSet.AddAsync(entity, cancellationToken);
        return result.Entity;
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken)
    {
        return await DbSet.AnyAsync(predicate, cancellationToken: cancellationToken);
    }
    
    public async Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken,
        params Expression<Func<TEntity, object>>[] lazySelectors)
    {
        return await DbSet
            .WithLazySelectors<TEntity, TId>(lazySelectors)
            .Where(predicate)
            .LongCountAsync(cancellationToken: cancellationToken);
    }

    public void Remove(TEntity entity)
    {
        DbSet.Remove(entity);
    }
}