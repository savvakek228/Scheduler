namespace Scheduler.Infrastructure.Messaging;

public sealed class TaskChannelOptions
{
    public const int DefaultCapacity = 256;

    public int Capacity { get; init; } = DefaultCapacity;
}
