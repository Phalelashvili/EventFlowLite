namespace EventFlowLite.Abstractions.Jobs;

// using exact same contracts as BackgroundJob barely has an advantage of allowing it to be mocked.
// we could implement more advanced mechanism like in EventFlow
public interface IRecurringJobScheduler
{
    // cron and timezone CAN be combined into single argument with convention that cron is always utc
    Task ScheduleAsync<TJob>(string jobId, string cron, TimeZoneInfo timeZone, CancellationToken cancellationToken)
        where TJob : IRecurringJob;

    Task CancelAsync(string jobId);
}