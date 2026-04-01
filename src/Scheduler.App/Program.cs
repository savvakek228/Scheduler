using Scheduler.App.Api;
using Scheduler.App.Components;
using Scheduler.App.Hosting;
using Scheduler.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSchedulerInfrastructure();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<OutboxRelayHostedService>();
builder.Services.AddHostedService<TaskProcessorHostedService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapSchedulerApi();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
