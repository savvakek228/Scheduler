using Scheduler.Application.Abstractions;
using Scheduler.Application.Messaging;
using Scheduler.Application.Persistence;
using Scheduler.Application.Processing;
using Scheduler.Domain.Outbox;

namespace Scheduler.Application.Processing;

public sealed class TaskMessageProcessor(
    ISchedulerPersistence persistence,
    ITaskExecutor executor,
    IClock clock,
    RetryPolicyOptions retryOptions) : ITaskMessageProcessor
{
    public async Task ProcessAsync(OutboxEnvelope envelope, CancellationToken cancellationToken)
    {
        var inserted = await persistence.TryInsertInboxAsync(envelope.MessageId, clock.UtcNow, cancellationToken);
        if (!inserted)
            return;

        var task = await persistence.GetTaskAsync(envelope.TaskId, cancellationToken);
        if (task is null)
        {
            await persistence.RemoveInboxAsync(envelope.MessageId, cancellationToken);
            return;
        }

        if (task.RunAt > clock.UtcNow)
        {
            await persistence.DeferUntilAsync(task.Id, task.RunAt, cancellationToken);
            await RequeueOutboxAsync(task.Id, envelope.PayloadJson, cancellationToken);
            return;
        }

        var running = await persistence.TrySetTaskRunningAsync(task.Id, task.Version, cancellationToken);
        if (!running)
        {
            await persistence.RemoveInboxAsync(envelope.MessageId, cancellationToken);
            return;
        }

        var executionAttempt = task.AttemptCount + 1;
        try
        {
            await executor.ExecuteAsync(task.Id, envelope.PayloadJson, executionAttempt, cancellationToken);
            await persistence.MarkTaskCompletedAsync(task.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            var updated = await persistence.GetTaskAsync(task.Id, cancellationToken);
            if (updated is null)
                return;

            var failedAttemptNumber = executionAttempt;
            if (failedAttemptNumber >= retryOptions.MaxAttempts)
            {
                await persistence.MoveTaskToDeadLetterAsync(
                    task.Id,
                    ex.Message,
                    failedAttemptNumber,
                    cancellationToken);
                return;
            }

            var delay = TimeSpan.FromTicks(retryOptions.BaseDelay.Ticks * (long)Math.Pow(2, failedAttemptNumber));
            var next = clock.UtcNow + delay;

            await persistence.ScheduleRetryAsync(
                task.Id,
                ex.Message,
                next,
                failedAttemptNumber,
                cancellationToken);

            await RequeueOutboxAsync(task.Id, envelope.PayloadJson, cancellationToken);
        }
    }

    private async Task RequeueOutboxAsync(Guid taskId, string payloadJson, CancellationToken cancellationToken)
    {
        var messageId = Guid.NewGuid();
        var outbox = new OutboxMessage
        {
            MessageId = messageId,
            TaskId = taskId,
            EnvelopeType = "ProcessTask",
            PayloadJson = payloadJson,
            Status = OutboxMessageStatus.PendingPublish,
            CreatedAt = clock.UtcNow
        };

        await persistence.EnqueueRetryOutboxAsync(outbox, cancellationToken);
        await persistence.TrySetTaskQueuedAsync(taskId, cancellationToken);
    }
}
