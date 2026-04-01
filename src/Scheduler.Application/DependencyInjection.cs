using Microsoft.Extensions.DependencyInjection;
using Scheduler.Application.Abstractions;
using Scheduler.Application.Outbox;
using Scheduler.Application.Processing;
using Scheduler.Application.Queries;
using Scheduler.Application.Scheduling;

namespace Scheduler.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSchedulerApplication(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<RetryPolicyOptions>(_ => new RetryPolicyOptions());
        services.AddScoped<IScheduleTaskService, ScheduleTaskService>();
        services.AddScoped<IOutboxRelayService, OutboxRelayService>();
        services.AddScoped<ITaskMessageProcessor, TaskMessageProcessor>();
        services.AddScoped<ISchedulerQueryService, SchedulerQueryService>();
        return services;
    }

    private sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
