
namespace Application.Realtime
{
    /// <summary>
    /// Represents a strongly typed real-time event propagated to connected clients.
    /// </summary>
    /// <typeparam name="TPayload">The payload type carried by the event.</typeparam>
    public abstract record RealtimeEvent<TPayload>(
        string Type,
        Guid ProjectId,
        DateTimeOffset OccurredAt,
        TPayload Payload);
}
