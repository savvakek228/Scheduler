namespace Scheduler.Domain.DeadLetters;

public sealed class DeadLetterItem
{
    public long Id { get; set; }
    public Guid TaskId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int FinalAttemptCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
