using Scheduler.Application.Messaging;

namespace Scheduler.Application.Processing;

public interface ITaskMessageProcessor
{
    Task ProcessAsync(OutboxEnvelope envelope, CancellationToken cancellationToken);
}
