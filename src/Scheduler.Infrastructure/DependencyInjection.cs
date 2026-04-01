using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Scheduler.Application;
using Scheduler.Application.Messaging;
using Scheduler.Application.Persistence;
using Scheduler.Application.Processing;
using Scheduler.Infrastructure.Messaging;
using Scheduler.Infrastructure.Persistence;
using Scheduler.Infrastructure.Processing;

namespace Scheduler.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSchedulerInfrastructure(this IServiceCollection services)
    {
        services.AddSchedulerApplication();
        services.Configure<TaskChannelOptions>(_ => { });
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("scheduler"));
        services.AddScoped<ISchedulerPersistence, EfSchedulerPersistence>();
        services.AddSingleton<ITaskProcessChannel, TaskProcessChannel>();
        services.AddScoped<ITaskExecutor, DefaultTaskExecutor>();
        return services;
    }
}
