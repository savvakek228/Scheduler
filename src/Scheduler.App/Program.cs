using Microsoft.EntityFrameworkCore;
using Scheduler.App.Api;
using Scheduler.App.Components;
using Scheduler.App.Hosting;
using Scheduler.Infrastructure;
using Scheduler.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSchedulerInfrastructure();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<OutboxRelayHostedService>();
builder.Services.AddHostedService<TaskProcessorHostedService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapSchedulerApi();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
