using Scheduler.Contracts.DeadLetters;
using Scheduler.Contracts.Diagnostics;
using Scheduler.Contracts.Tasks;

namespace Scheduler.Application.Queries;

public interface ISchedulerQueryService
{
    Task<IReadOnlyList<TaskSummaryDto>> GetRecentTasksAsync(int take, CancellationToken cancellationToken);

    Task<TaskResponse?> GetTaskAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<DeadLetterDto>> GetDeadLettersAsync(int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<OutboxItemDto>> GetOutboxDiagnosticsAsync(int take, CancellationToken cancellationToken);
}
