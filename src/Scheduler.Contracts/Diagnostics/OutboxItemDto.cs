namespace Scheduler.Contracts.Diagnostics;

public sealed record OutboxItemDto(
    long Id,
    Guid MessageId,
    Guid TaskId,
    string EnvelopeType,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt);
