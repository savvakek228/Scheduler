using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        services.AddDbContext<AppDbContext>((sp, o) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var configured = config["Scheduler:SqlitePath"];
            var dbPath = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(sp.GetRequiredService<IHostEnvironment>().ContentRootPath, "scheduler.sqlite")
                : configured;
            o.UseSqlite($"Data Source={dbPath}");
        });
        services.AddScoped<ISchedulerPersistence, EfSchedulerPersistence>();
        services.AddSingleton<ITaskProcessChannel, TaskProcessChannel>();
        services.AddScoped<ITaskExecutor, DefaultTaskExecutor>();
        return services;
    }
}
