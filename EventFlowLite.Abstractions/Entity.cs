namespace EventFlowLite.Abstractions;

public interface IEntityBase
{
    object Id { get; }
}

public interface IEntity<out TId> : IEntityBase
{
    new TId Id { get; }
}

public abstract class Entity<TId> : IEntity<TId>
{
    protected Entity(TId id)
    {
        Id = id;
    }

    public TId Id { get; }
    object IEntityBase.Id => Id;
}