using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Scheduler.Application.Messaging;

namespace Scheduler.Infrastructure.Messaging;

public sealed class TaskProcessChannel(IOptions<TaskChannelOptions> options) : ITaskProcessChannel
{
    private readonly Channel<OutboxEnvelope> _channel = Channel.CreateBounded<OutboxEnvelope>(
        new BoundedChannelOptions(options.Value.Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

    public ValueTask PublishAsync(OutboxEnvelope envelope, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(envelope, cancellationToken);

    public ValueTask<OutboxEnvelope> ReadAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAsync(cancellationToken);
}
