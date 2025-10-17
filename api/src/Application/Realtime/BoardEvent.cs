namespace Application.Realtime
{
    // Generic base
    public abstract record BoardEvent<TPayload>(
        string Type,
        Guid ProjectId,
        DateTimeOffset OccurredAt,
        TPayload Payload);

    // Payloads
    public sealed record TaskCreatedPayload(
        Guid TaskId, Guid ColumnId, Guid LaneId, string Title, string Description, decimal SortKey);

    public sealed record TaskEditedPayload(
        Guid TaskId, string? NewTitle, string? NewDescription, DateTimeOffset? NewDueDate);

    public sealed record TaskMovedPayload(
        Guid TaskId, Guid FromLaneId, Guid FromColumnId, Guid ToLaneId, Guid ToColumnId, decimal SortKey);

    // Concrete events
    public sealed record TaskCreatedEvent(Guid ProjectId, TaskCreatedPayload Payload)
        : BoardEvent<TaskCreatedPayload>("task.created", ProjectId, DateTimeOffset.UtcNow, Payload);

    public sealed record TaskEditedEvent(Guid ProjectId, TaskEditedPayload Payload)
        : BoardEvent<TaskEditedPayload>("task.updated", ProjectId, DateTimeOffset.UtcNow, Payload);

    public sealed record TaskMovedEvent(Guid ProjectId, TaskMovedPayload Payload)
        : BoardEvent<TaskMovedPayload>("task.moved", ProjectId, DateTimeOffset.UtcNow, Payload);
}
