using Scheduler.Domain.DeadLetters;
using Scheduler.Domain.Inbox;
using Scheduler.Domain.Outbox;
using Scheduler.Domain.Tasks;

namespace Scheduler.Application.Persistence;

public interface ISchedulerPersistence
{
    Task<ScheduledTask?> FindTaskByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    Task AddTaskWithOutboxAsync(ScheduledTask task, OutboxMessage outbox, CancellationToken cancellationToken);

    Task<IReadOnlyList<OutboxMessage>> GetPendingOutboxAsync(int take, CancellationToken cancellationToken);

    Task MarkOutboxPublishedAsync(long outboxId, DateTimeOffset publishedAt, CancellationToken cancellationToken);

    /// <summary>Returns false if message was already processed (duplicate).</summary>
    Task<bool> TryInsertInboxAsync(Guid messageId, DateTimeOffset receivedAt, CancellationToken cancellationToken);

    Task RemoveInboxAsync(Guid messageId, CancellationToken cancellationToken);

    Task<ScheduledTask?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken);

    Task<bool> TrySetTaskQueuedAsync(Guid taskId, CancellationToken cancellationToken);

    /// <summary>When RunAt is still in the future: keep attempt count, delay visibility.</summary>
    Task DeferUntilAsync(Guid taskId, DateTimeOffset earliestRunAt, CancellationToken cancellationToken);

    Task<bool> TrySetTaskRunningAsync(Guid taskId, int expectedVersion, CancellationToken cancellationToken);

    Task MarkTaskCompletedAsync(Guid taskId, CancellationToken cancellationToken);

    Task ScheduleRetryAsync(
        Guid taskId,
        string error,
        DateTimeOffset nextAttemptAt,
        int failedAttemptNumber,
        CancellationToken cancellationToken);

    Task EnqueueRetryOutboxAsync(OutboxMessage outbox, CancellationToken cancellationToken);

    Task MoveTaskToDeadLetterAsync(Guid taskId, string reason, int finalAttempts, CancellationToken cancellationToken);

    Task<IReadOnlyList<ScheduledTask>> ListTasksAsync(int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<DeadLetterItem>> ListDeadLettersAsync(int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<OutboxMessage>> ListOutboxDiagnosticsAsync(int take, CancellationToken cancellationToken);

    Task<bool> TryReplayDeadLetterAsync(long deadLetterId, CancellationToken cancellationToken);
}
