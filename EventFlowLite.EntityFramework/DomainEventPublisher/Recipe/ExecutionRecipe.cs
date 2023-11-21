namespace EventFlowLite.EntityFramework.DomainEventPublisher.Recipe;

public class ExecutionRecipe
{
    public readonly DomainEventPublishingStrategy Strategy;

    private ExecutionRecipe? _fallbackRecipe;
    public ExecutionRecipe? FallbackRecipe => _fallbackRecipe;
    
    private TimeSpan? _timeout;
    public TimeSpan? Timeout => _timeout;

    public ExecutionRecipe(DomainEventPublishingStrategy strategy)
    {
        Strategy = strategy;
    }

    public virtual ExecutionRecipe FallbackTo(ExecutionRecipe fallbackRecipe)
    {
        _fallbackRecipe = fallbackRecipe;
        return this;
    }

    public virtual ExecutionRecipe WithTimeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }
}