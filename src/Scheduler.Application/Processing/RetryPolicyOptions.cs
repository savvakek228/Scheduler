namespace Scheduler.Application.Processing;

public sealed class RetryPolicyOptions
{
    public const int DefaultMaxAttempts = 5;

    public int MaxAttempts { get; init; } = DefaultMaxAttempts;

    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromSeconds(2);
}
