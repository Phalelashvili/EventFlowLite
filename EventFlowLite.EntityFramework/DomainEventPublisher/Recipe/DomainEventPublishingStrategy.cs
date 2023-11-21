namespace EventFlowLite.EntityFramework.DomainEventPublisher.Recipe;

public enum DomainEventPublishingStrategy
{
    Local,
    RedisHangfire,
    SqlHangfire
}