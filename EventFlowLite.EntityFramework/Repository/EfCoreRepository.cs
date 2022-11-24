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

    public Task SaveChangesAsync()
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

        return DbContext.SaveChangesAsync();
    }

    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool track = false,
        params Expression<Func<TEntity, object>>[] lazySelectors)
    {
        return DbSet
            .WithLazySelectors<TEntity, TId>(lazySelectors)
            .WithTracking<TEntity, TId>(track)
            .FirstOrDefaultAsync(predicate);
    }

    public async Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> predicate, bool track = false,
        params Expression<Func<TEntity, object>>[] lazySelectors)
    {
        var entity = await FirstOrDefaultAsync(predicate, track, lazySelectors);
        if (entity == default)
            throw new ApplicationException($"no matching {typeof(TEntity).Name} found where {predicate}");
        return entity;
    }

    public Task<List<TEntity>> QueryListAsync(Expression<Func<TEntity, bool>> predicate,
        params Expression<Func<TEntity, object>>[] lazySelectors)
    {
        return DbSet
            .WithLazySelectors<TEntity, TId>(lazySelectors)
            .Where(predicate)
            .ToListAsync();
    }

    public Task<List<TEntity>> QueryListAsync(IEnumerable<Expression<Func<TEntity, bool>>> predicates,
        params Expression<Func<TEntity, object>>[] lazySelectors)
    {
        var q = DbSet.WithLazySelectors<TEntity, TId>(lazySelectors).AsQueryable();

        foreach (var predicate in predicates)
            q = q.Where(predicate);

        return q.ToListAsync();
    }

    public ValueTask<TEntity?> FindManyAsync(params TId[] values)
    {
        return DbSet.FindAsync(values);
    }

    public ValueTask<TEntity?> FindAsync(TId value, bool track = false)
    {
        return DbSet.WithTracking<TEntity, TId>(track).FindAsync(value);
    }

    public ValueTask<TEntity?> FindAsync(TId value, CancellationToken cancellationToken, bool track = false)
    {
        return FindAsync(value, track);
    }
    // using cancellation token as argument caused some issues with object[] overload. not important rn. TODO

    public async ValueTask<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var result = await DbSet.AddAsync(entity, cancellationToken);
        return result.Entity;
    }

    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await DbSet.AnyAsync(predicate);
    }
    
    public async Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate,
        params Expression<Func<TEntity, object>>[] lazySelectors)
    {
        return await DbSet
            .WithLazySelectors<TEntity, TId>(lazySelectors)
            .Where(predicate)
            .LongCountAsync();
    }
}