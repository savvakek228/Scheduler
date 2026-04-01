namespace Scheduler.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
