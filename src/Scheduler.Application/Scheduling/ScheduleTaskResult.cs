namespace Scheduler.Application.Scheduling;

public sealed record ScheduleTaskResult(Guid TaskId, bool WasCreated);
