namespace Scheduler.Domain.Inbox;

public sealed class InboxRecord
{
    public long Id { get; set; }
    public Guid MessageId { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
}
