using Scheduler.Application.Persistence;
using Scheduler.Contracts.DeadLetters;
using Scheduler.Contracts.Diagnostics;
using Scheduler.Contracts.Tasks;
using Scheduler.Domain.Tasks;

namespace Scheduler.Application.Queries;

public sealed class SchedulerQueryService(ISchedulerPersistence persistence) : ISchedulerQueryService
{
    public async Task<IReadOnlyList<TaskSummaryDto>> GetRecentTasksAsync(int take, CancellationToken cancellationToken)
    {
        var list = await persistence.ListTasksAsync(take, cancellationToken);
        return list.Select(t => new TaskSummaryDto(
                t.Id,
                t.RunAt,
                t.Status.ToString(),
                t.AttemptCount,
                t.CreatedAt))
            .ToList();
    }

    public async Task<TaskResponse?> GetTaskAsync(Guid id, CancellationToken cancellationToken)
    {
        var t = await persistence.GetTaskAsync(id, cancellationToken);
        if (t is null)
            return null;

        return new TaskResponse(
            t.Id,
            t.Payload,
            t.RunAt,
            t.Status.ToString(),
            t.IdempotencyKey,
            t.AttemptCount,
            t.NextAttemptAt,
            t.LastError,
            t.CreatedAt);
    }

    public async Task<IReadOnlyList<DeadLetterDto>> GetDeadLettersAsync(int take, CancellationToken cancellationToken)
    {
        var list = await persistence.ListDeadLettersAsync(take, cancellationToken);
        return list.Select(d => new DeadLetterDto(
                d.Id,
                d.TaskId,
                d.Reason,
                d.FinalAttemptCount,
                d.CreatedAt))
            .ToList();
    }

    public async Task<IReadOnlyList<OutboxItemDto>> GetOutboxDiagnosticsAsync(int take, CancellationToken cancellationToken)
    {
        var list = await persistence.ListOutboxDiagnosticsAsync(take, cancellationToken);
        return list.Select(o => new OutboxItemDto(
                o.Id,
                o.MessageId,
                o.TaskId,
                o.EnvelopeType,
                o.Status.ToString(),
                o.CreatedAt,
                o.PublishedAt))
            .ToList();
    }
}
