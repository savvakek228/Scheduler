namespace Scheduler.Application.Messaging;

public sealed record OutboxEnvelope(
    Guid MessageId,
    Guid TaskId,
    string EnvelopeType,
    string PayloadJson);
