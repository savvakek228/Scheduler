namespace Scheduler.Domain.Outbox;

public sealed class OutboxMessage
{
    public long Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid TaskId { get; set; }
    public string EnvelopeType { get; set; } = "ProcessTask";
    public string PayloadJson { get; set; } = "{}";
    public OutboxMessageStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
