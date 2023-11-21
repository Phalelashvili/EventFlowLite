namespace EventFlowLite.EntityFramework.DomainEventPublisher.Recipe;

public static class ExecutionRecipeFactory
{
    public static ExecutionRecipe Create(DomainEventPublishingStrategy strategy)
    {
        return strategy switch
        {
            DomainEventPublishingStrategy.Local => new ExecutionRecipe(strategy),
            DomainEventPublishingStrategy.RedisHangfire => new RedisHangfireExecutionRecipe(),
            DomainEventPublishingStrategy.SqlHangfire => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null)
        };
    }
}