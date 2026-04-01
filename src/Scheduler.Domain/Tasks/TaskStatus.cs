namespace Scheduler.Domain.Tasks;

public enum TaskStatus
{
    Pending = 0,
    Queued = 1,
    Running = 2,
    Completed = 3,
    DeadLetter = 4
}
