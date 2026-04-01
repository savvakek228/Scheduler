using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Scheduler.Contracts.Tasks;
using Xunit;

namespace Scheduler.Tests.Integration;

public sealed class ApiSchedulingTests : IClassFixture<WebAppFactory>
{
    private readonly HttpClient _client;

    public ApiSchedulingTests(WebAppFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task PostTasks_IdempotencyKey_ReturnsSameTaskId()
    {
        var body = new CreateTaskRequest("{\"k\":1}", DateTimeOffset.UtcNow.AddSeconds(-1), "idem-1");

        var r1 = await _client.PostAsJsonAsync("/api/tasks", body);
        var r2 = await _client.PostAsJsonAsync("/api/tasks", body);

        Assert.Equal(HttpStatusCode.Created, r1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, r2.StatusCode);

        var id1 = await r1.Content.ReadFromJsonAsync<TaskCreationResponse>();
        var id2 = await r2.Content.ReadFromJsonAsync<TaskCreationResponse>();

        Assert.NotNull(id1);
        Assert.NotNull(id2);
        Assert.Equal(id1!.TaskId, id2!.TaskId);
        Assert.True(id1.WasCreated);
        Assert.False(id2.WasCreated);
    }

    [Fact]
    public async Task PostTasks_EventuallyCompletesWhenRunAtIsPast()
    {
        var payload = "{\"hello\":\"world\"}";
        var body = new CreateTaskRequest(payload, DateTimeOffset.UtcNow.AddMinutes(-5), null);
        var create = await _client.PostAsJsonAsync("/api/tasks", body);
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<TaskCreationResponse>();
        Assert.NotNull(created);

        TaskResponse? task = null;
        for (var i = 0; i < 200; i++)
        {
            await Task.Delay(50);
            var res = await _client.GetAsync($"/api/tasks/{created!.TaskId}");
            if (res.StatusCode != HttpStatusCode.OK)
                continue;
            task = await res.Content.ReadFromJsonAsync<TaskResponse>();
            if (task?.Status == "Completed")
                break;
        }

        Assert.NotNull(task);
        Assert.Equal("Completed", task!.Status);
    }

    private sealed record TaskCreationResponse(
        [property: JsonPropertyName("taskId")] Guid TaskId,
        [property: JsonPropertyName("wasCreated")] bool WasCreated);
}
