using System.Linq.Expressions;

namespace EventFlowLite.Abstractions.Jobs;

// using exact same contracts as BackgroundJob barely has an advantage of allowing it to be mocked.
// we could implement more advanced mechanism like in EventFlow
public interface IJobScheduler
{
    Task<IJobId> ScheduleNowAsync<TJob>(Expression<Action<TJob>> methodCall, CancellationToken cancellationToken)
        where TJob : IJob;

    Task<IJobId> ScheduleAsync<TJob>(Expression<Action<TJob>> methodCall, DateTimeOffset runAt,
        CancellationToken cancellationToken)
        where TJob : IJob;

    Task<IJobId> ScheduleAsync<TJob>(Expression<Action<TJob>> methodCall, TimeSpan delay,
        CancellationToken cancellationToken)
        where TJob : IJob;
}