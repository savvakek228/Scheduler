namespace Scheduler.Contracts.Tasks;

public sealed record TaskResponse(
    Guid Id,
    string Payload,
    DateTimeOffset RunAt,
    string Status,
    string? IdempotencyKey,
    int AttemptCount,
    DateTimeOffset? NextAttemptAt,
    string? LastError,
    DateTimeOffset CreatedAt);
