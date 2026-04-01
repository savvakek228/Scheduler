namespace Scheduler.Application.Outbox;

public interface IOutboxRelayService
{
    Task RelayBatchAsync(int batchSize, CancellationToken cancellationToken);
}
