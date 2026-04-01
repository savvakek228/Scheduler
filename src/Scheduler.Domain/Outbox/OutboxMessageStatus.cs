namespace Scheduler.Domain.Outbox;

public enum OutboxMessageStatus
{
    PendingPublish = 0,
    Published = 1
}
