using Scheduler.Application.Messaging;
using Scheduler.Application.Processing;

namespace Scheduler.App.Hosting;

public sealed class TaskProcessorHostedService(
    ITaskProcessChannel channel,
    IServiceScopeFactory scopeFactory,
    ILogger<TaskProcessorHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var envelope = await channel.ReadAsync(stoppingToken).ConfigureAwait(false);
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var processor = scope.ServiceProvider.GetRequiredService<ITaskMessageProcessor>();
                    await processor.ProcessAsync(envelope, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Task processing failed for message {MessageId} task {TaskId}",
                        envelope.MessageId,
                        envelope.TaskId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
    }
}
