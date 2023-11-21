namespace EventFlowLite.Abstractions;

public interface IFeatureFlags
{
    bool LocalDomainEventHandlingEnabled { get; }
}