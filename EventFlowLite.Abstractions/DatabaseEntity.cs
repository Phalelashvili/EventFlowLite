using System.ComponentModel.DataAnnotations;

namespace EventFlowLite.Abstractions;

public interface IDatabaseEntityBase
{
}

public interface IDatabaseEntity<out TId> : IDatabaseEntityBase, IEntity<TId>
    where TId : IComparable
{
    DateTime DateTimeCreated { get; }
    DateTime DateTimeUpdated { get; }
    int ConcurrencyVersion { get; }
}

public abstract class DatabaseEntity<TId> : IDatabaseEntity<TId>
    where TId : IComparable
{
    public DateTime DateTimeCreated { get; set; }
    public DateTime DateTimeUpdated { get; set; }

    [ConcurrencyCheck] public int ConcurrencyVersion { get; set; }

    public abstract TId Id { get; protected set; }
    object IEntityBase.Id => Id;
}