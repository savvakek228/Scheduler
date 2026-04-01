using System.Text.Json;
using Scheduler.Application.Processing;

namespace Scheduler.Infrastructure.Processing;

public sealed class DefaultTaskExecutor : ITaskExecutor
{
    public Task ExecuteAsync(Guid taskId, string payloadJson, int executionAttempt, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;
        var userPayload = root.TryGetProperty("payload", out var p) ? p.GetString() ?? string.Empty : string.Empty;

        var failUntil = 0;
        if (root.TryGetProperty("failUntilAttempt", out var outFail))
            failUntil = outFail.GetInt32();

        if (!string.IsNullOrWhiteSpace(userPayload) && userPayload.TrimStart().StartsWith('{'))
        {
            try
            {
                using var inner = JsonDocument.Parse(userPayload);
                if (inner.RootElement.TryGetProperty("failUntilAttempt", out var innerFail))
                    failUntil = innerFail.GetInt32();
            }
            catch (JsonException)
            {
                // ignore malformed inner JSON
            }
        }

        if (failUntil > 0 && executionAttempt < failUntil)
        {
            throw new InvalidOperationException(
                $"Simulated failure: executionAttempt {executionAttempt} is below failUntilAttempt {failUntil}.");
        }

        return Task.CompletedTask;
    }
}
