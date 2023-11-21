namespace EventFlowLite.EntityFramework.DomainEventPublisher.Recipe;

public class RedisHangfireExecutionRecipe : ExecutionRecipe
{
    public RedisHangfireExecutionRecipe() : base(DomainEventPublishingStrategy.RedisHangfire)
    {
    }

    public override ExecutionRecipe WithTimeout(TimeSpan timeout)
    {
        throw new NotSupportedException();
    }
}