
namespace Application.Realtime
{
    public abstract record RealtimeEvent<TPayload>(
        string Type,
        Guid ProjectId,
        DateTimeOffset OccurredAt,
        TPayload Payload);
}
