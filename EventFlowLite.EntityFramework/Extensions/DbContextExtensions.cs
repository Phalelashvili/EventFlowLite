using Microsoft.EntityFrameworkCore;

namespace EventFlowLite.EntityFramework.Extensions;

public static class DbContextExtensions
{
    public static bool EntitiesWereAdded(this DbContext dbContext)
    {
        return dbContext.ChangeTracker.Entries().Any(e => e.State is EntityState.Added);
    }
}