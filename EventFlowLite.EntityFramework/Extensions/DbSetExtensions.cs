using System.Linq.Expressions;
using EventFlowLite.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace EventFlowLite.EntityFramework.Extensions;

public static class DbSetExtensions
{
    public static DbSet<TEntity> WithLazySelectors<TEntity, TId>(this DbSet<TEntity> dbSet,
        params Expression<Func<TEntity, object>>[] lazySelectors)
        where TEntity : DatabaseEntity<TId> where TId : IComparable
    {
        foreach (var exp in lazySelectors)
            dbSet.Include(exp);
        return dbSet;
    }

    public static DbSet<TEntity> WithTracking<TEntity, TId>(this DbSet<TEntity> dbSet, bool tracking)
        where TEntity : DatabaseEntity<TId> where TId : IComparable
    {
        if (tracking)
            dbSet.AsTracking();
        else
            dbSet.AsNoTracking();

        return dbSet;
    }
}