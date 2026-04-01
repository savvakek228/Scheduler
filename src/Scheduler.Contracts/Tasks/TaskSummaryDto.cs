namespace Scheduler.Contracts.Tasks;

public sealed record TaskSummaryDto(
    Guid Id,
    DateTimeOffset RunAt,
    string Status,
    int AttemptCount,
    DateTimeOffset CreatedAt);
