namespace Scheduler.Application.Messaging;

public interface ITaskProcessChannel
{
    ValueTask PublishAsync(OutboxEnvelope envelope, CancellationToken cancellationToken);
    ValueTask<OutboxEnvelope> ReadAsync(CancellationToken cancellationToken);
}
