using Scheduler.Application.Outbox;

namespace Scheduler.App.Hosting;

public sealed class OutboxRelayHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxRelayHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var relay = scope.ServiceProvider.GetRequiredService<IOutboxRelayService>();
                    await relay.RelayBatchAsync(batchSize: 50, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Outbox relay iteration failed");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
    }
}
