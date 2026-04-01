namespace Scheduler.Application.Processing;

public interface ITaskExecutor
{
    /// <summary>Simulated work. Throws to trigger retry policy.</summary>
    Task ExecuteAsync(
        Guid taskId,
        string payloadJson,
        int executionAttempt,
        CancellationToken cancellationToken);
}
