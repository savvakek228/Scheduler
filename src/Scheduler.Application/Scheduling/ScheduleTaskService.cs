using System.Text.Json;
using Scheduler.Application.Abstractions;
using Scheduler.Application.Persistence;
using Scheduler.Domain.Outbox;
using Scheduler.Domain.Tasks;
using TaskStatus = Scheduler.Domain.Tasks.TaskStatus;

namespace Scheduler.Application.Scheduling;

public sealed class ScheduleTaskService(
    ISchedulerPersistence persistence,
    IClock clock) : IScheduleTaskService
{
    public async Task<ScheduleTaskResult> ScheduleAsync(
        string payload,
        DateTimeOffset runAt,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await persistence.FindTaskByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (existing is not null)
                return new ScheduleTaskResult(existing.Id, WasCreated: false);
        }

        var now = clock.UtcNow;
        var taskId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var task = new ScheduledTask
        {
            Id = taskId,
            Payload = payload,
            RunAt = runAt,
            Status = TaskStatus.Pending,
            IdempotencyKey = idempotencyKey,
            AttemptCount = 0,
            CreatedAt = now,
            Version = 0
        };

        var outbox = new OutboxMessage
        {
            MessageId = messageId,
            TaskId = taskId,
            EnvelopeType = "ProcessTask",
            PayloadJson = JsonSerializer.Serialize(new { taskId, payload }),
            Status = OutboxMessageStatus.PendingPublish,
            CreatedAt = now
        };

        await persistence.AddTaskWithOutboxAsync(task, outbox, cancellationToken);
        return new ScheduleTaskResult(taskId, WasCreated: true);
    }
}
