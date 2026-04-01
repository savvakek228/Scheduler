namespace Scheduler.Application.Scheduling;

public interface IScheduleTaskService
{
    Task<ScheduleTaskResult> ScheduleAsync(
        string payload,
        DateTimeOffset runAt,
        string? idempotencyKey,
        CancellationToken cancellationToken);
}
