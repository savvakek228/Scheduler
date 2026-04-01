namespace Scheduler.Contracts.Tasks;

public sealed record CreateTaskRequest(
    string Payload,
    DateTimeOffset RunAt,
    string? IdempotencyKey);
