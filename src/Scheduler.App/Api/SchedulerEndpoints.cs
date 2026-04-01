using Scheduler.Application.Persistence;
using Scheduler.Application.Scheduling;
using Scheduler.Application.Queries;
using Scheduler.Contracts.Tasks;

namespace Scheduler.App.Api;

public static class SchedulerEndpoints
{
    public static WebApplication MapSchedulerApi(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        api.MapPost("/tasks", async (
                CreateTaskRequest body,
                IScheduleTaskService scheduling,
                CancellationToken ct) =>
            {
                var result = await scheduling.ScheduleAsync(body.Payload, body.RunAt, body.IdempotencyKey, ct);
                return Results.Created($"/api/tasks/{result.TaskId}", new { result.TaskId, result.WasCreated });
            });

        api.MapGet("/tasks", async (ISchedulerQueryService queries, int? take, CancellationToken ct) =>
        {
            var list = await queries.GetRecentTasksAsync(take ?? 100, ct);
            return Results.Ok(list);
        });

        api.MapGet("/tasks/{id:guid}", async (Guid id, ISchedulerQueryService queries, CancellationToken ct) =>
        {
            var task = await queries.GetTaskAsync(id, ct);
            return task is null ? Results.NotFound() : Results.Ok(task);
        });

        api.MapGet("/dead-letters", async (ISchedulerQueryService queries, int? take, CancellationToken ct) =>
        {
            var list = await queries.GetDeadLettersAsync(take ?? 100, ct);
            return Results.Ok(list);
        });

        api.MapPost("/dead-letters/{id:long}/replay", async (
                long id,
                ISchedulerPersistence persistence,
                CancellationToken ct) =>
        {
            var ok = await persistence.TryReplayDeadLetterAsync(id, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        });

        api.MapGet("/diagnostics/outbox", async (ISchedulerQueryService queries, int? take, CancellationToken ct) =>
        {
            var list = await queries.GetOutboxDiagnosticsAsync(take ?? 100, ct);
            return Results.Ok(list);
        });

        return app;
    }
}
