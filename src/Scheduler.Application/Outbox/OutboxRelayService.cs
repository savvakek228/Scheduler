using Scheduler.Application.Abstractions;
using Scheduler.Application.Messaging;
using Scheduler.Application.Persistence;

namespace Scheduler.Application.Outbox;

public sealed class OutboxRelayService(
    ISchedulerPersistence persistence,
    ITaskProcessChannel channel,
    IClock clock) : IOutboxRelayService
{
    public async Task RelayBatchAsync(int batchSize, CancellationToken cancellationToken)
    {
        var pending = await persistence.GetPendingOutboxAsync(batchSize, cancellationToken);
        foreach (var msg in pending)
        {
            var envelope = new OutboxEnvelope(
                msg.MessageId,
                msg.TaskId,
                msg.EnvelopeType,
                msg.PayloadJson);

            await channel.PublishAsync(envelope, cancellationToken);

            await persistence.MarkOutboxPublishedAsync(msg.Id, clock.UtcNow, cancellationToken);
            await persistence.TrySetTaskQueuedAsync(msg.TaskId, cancellationToken);
        }
    }
}
