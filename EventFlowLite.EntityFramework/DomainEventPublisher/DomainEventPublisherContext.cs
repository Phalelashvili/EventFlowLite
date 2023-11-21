namespace EventFlowLite.EntityFramework.DomainEventPublisher;

internal class DomainEventPublisherContext
{
    private int _nestLevel;
    public int NestLevel => _nestLevel;

    public DomainEventPublisherContext()
    {
        _nestLevel = -1;
    }

    public void IncrementNestLevel()
    {
        // Interlocked.Increment(ref _nestLevel); // don't need this?
        // race conditions shouldn't really happen here since you can't won't be running commands in parallel, rather,
        // you'll be running them "in chain". by the time next one gets to increment stage, previous should already be
        // awaiting next to finish
     
        _nestLevel++;
    }
}