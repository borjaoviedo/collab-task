namespace Application.Realtime
{
    public abstract record BoardEvent<TPayload>(
        string Type,
        Guid ProjectId,
        DateTimeOffset OccurredAt,
        TPayload Payload);
}
