using Microsoft.EntityFrameworkCore;
using Scheduler.Application.Abstractions;
using Scheduler.Application.Persistence;
using Scheduler.Domain.DeadLetters;
using Scheduler.Domain.Inbox;
using Scheduler.Domain.Outbox;
using Scheduler.Domain.Tasks;
using TaskStatus = Scheduler.Domain.Tasks.TaskStatus;

namespace Scheduler.Infrastructure.Persistence;

public sealed class EfSchedulerPersistence(AppDbContext db, IClock clock) : ISchedulerPersistence
{
    public async Task<ScheduledTask?> FindTaskByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        await db.ScheduledTasks.AsNoTracking()
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task AddTaskWithOutboxAsync(ScheduledTask task, OutboxMessage outbox, CancellationToken cancellationToken)
    {
        db.ScheduledTasks.Add(task);
        db.OutboxMessages.Add(outbox);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingOutboxAsync(int take, CancellationToken cancellationToken) =>
        await db.OutboxMessages.AsNoTracking()
            .Where(o => o.Status == OutboxMessageStatus.PendingPublish)
            .OrderBy(o => o.Id)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task MarkOutboxPublishedAsync(long outboxId, DateTimeOffset publishedAt, CancellationToken cancellationToken)
    {
        await db.OutboxMessages.Where(o => o.Id == outboxId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(o => o.Status, OutboxMessageStatus.Published)
                    .SetProperty(o => o.PublishedAt, publishedAt),
                cancellationToken);
    }

    public async Task<bool> TryInsertInboxAsync(Guid messageId, DateTimeOffset receivedAt, CancellationToken cancellationToken)
    {
        db.InboxRecords.Add(new InboxRecord { MessageId = messageId, ReceivedAt = receivedAt });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task RemoveInboxAsync(Guid messageId, CancellationToken cancellationToken)
    {
        await db.InboxRecords.Where(r => r.MessageId == messageId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public Task<ScheduledTask?> GetTaskAsync(Guid taskId, CancellationToken cancellationToken) =>
        db.ScheduledTasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

    public async Task<bool> TrySetTaskQueuedAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var fromPending = await db.ScheduledTasks
            .Where(t => t.Id == taskId && t.Status == TaskStatus.Pending)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.Status, TaskStatus.Queued),
                cancellationToken);
        if (fromPending > 0)
            return true;

        return await db.ScheduledTasks.AnyAsync(t => t.Id == taskId && t.Status == TaskStatus.Queued, cancellationToken);
    }

    public Task DeferUntilAsync(Guid taskId, DateTimeOffset earliestRunAt, CancellationToken cancellationToken) =>
        db.ScheduledTasks.Where(t => t.Id == taskId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(t => t.Status, TaskStatus.Queued)
                    .SetProperty(t => t.NextAttemptAt, earliestRunAt),
                cancellationToken);

    public async Task<bool> TrySetTaskRunningAsync(Guid taskId, int expectedVersion, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var task = await db.ScheduledTasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null
            || task.Version != expectedVersion
            || (task.Status != TaskStatus.Queued && task.Status != TaskStatus.Pending)
            || (task.NextAttemptAt.HasValue && task.NextAttemptAt > now))
            return false;

        task.Status = TaskStatus.Running;
        task.Version++;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task MarkTaskCompletedAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await db.ScheduledTasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
            return;
        task.Status = TaskStatus.Completed;
        task.LastError = null;
        task.NextAttemptAt = null;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ScheduleRetryAsync(
        Guid taskId,
        string error,
        DateTimeOffset nextAttemptAt,
        int failedAttemptNumber,
        CancellationToken cancellationToken)
    {
        await db.ScheduledTasks.Where(t => t.Id == taskId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(t => t.Status, TaskStatus.Queued)
                    .SetProperty(t => t.LastError, error)
                    .SetProperty(t => t.NextAttemptAt, nextAttemptAt)
                    .SetProperty(t => t.AttemptCount, failedAttemptNumber)
                    .SetProperty(t => t.Version, t => t.Version + 1),
                cancellationToken);
    }

    public async Task EnqueueRetryOutboxAsync(OutboxMessage outbox, CancellationToken cancellationToken)
    {
        db.OutboxMessages.Add(outbox);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MoveTaskToDeadLetterAsync(
        Guid taskId,
        string reason,
        int finalAttempts,
        CancellationToken cancellationToken)
    {
        await db.DeadLetterItems.AddAsync(
            new DeadLetterItem
            {
                TaskId = taskId,
                Reason = reason,
                FinalAttemptCount = finalAttempts,
                CreatedAt = clock.UtcNow
            },
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        await db.ScheduledTasks.Where(t => t.Id == taskId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(t => t.Status, TaskStatus.DeadLetter),
                cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduledTask>> ListTasksAsync(int take, CancellationToken cancellationToken) =>
        await db.ScheduledTasks.AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DeadLetterItem>> ListDeadLettersAsync(int take, CancellationToken cancellationToken) =>
        await db.DeadLetterItems.AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OutboxMessage>> ListOutboxDiagnosticsAsync(int take, CancellationToken cancellationToken) =>
        await db.OutboxMessages.AsNoTracking()
            .OrderByDescending(o => o.Id)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<bool> TryReplayDeadLetterAsync(long deadLetterId, CancellationToken cancellationToken)
    {
        var item = await db.DeadLetterItems.FirstOrDefaultAsync(d => d.Id == deadLetterId, cancellationToken);
        if (item is null)
            return false;

        var taskId = item.TaskId;
        db.DeadLetterItems.Remove(item);

        var taskPayload = await db.ScheduledTasks.AsNoTracking()
            .Where(t => t.Id == taskId)
            .Select(t => t.Payload)
            .SingleOrDefaultAsync(cancellationToken);

        var messageId = Guid.NewGuid();
        var outbox = new OutboxMessage
        {
            MessageId = messageId,
            TaskId = taskId,
            EnvelopeType = "ProcessTask",
            PayloadJson = System.Text.Json.JsonSerializer.Serialize(new { taskId, payload = taskPayload ?? string.Empty }),
            Status = OutboxMessageStatus.PendingPublish,
            CreatedAt = clock.UtcNow
        };

        await db.ScheduledTasks.Where(t => t.Id == taskId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(t => t.Status, TaskStatus.Queued)
                    .SetProperty(t => t.AttemptCount, 0)
                    .SetProperty(t => t.LastError, (string?)null)
                    .SetProperty(t => t.NextAttemptAt, (DateTimeOffset?)null)
                    .SetProperty(t => t.Version, t => t.Version + 1),
                cancellationToken);

        db.OutboxMessages.Add(outbox);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
