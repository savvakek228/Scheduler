namespace Scheduler.Domain.Tasks;

public sealed class ScheduledTask
{
    public Guid Id { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset RunAt { get; set; }
    public TaskStatus Status { get; set; }
    public string? IdempotencyKey { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? NextAttemptAt { get; set; }
    public string? LastError { get; set; }
    public int Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
