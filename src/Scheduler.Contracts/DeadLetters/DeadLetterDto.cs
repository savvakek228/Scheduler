namespace Scheduler.Contracts.DeadLetters;

public sealed record DeadLetterDto(
    long Id,
    Guid TaskId,
    string Reason,
    int FinalAttemptCount,
    DateTimeOffset CreatedAt);
