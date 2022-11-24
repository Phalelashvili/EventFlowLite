namespace EventFlowLite.Abstractions.Jobs;

public interface IRecurringJob
{
    Task ExecuteAsync();
}