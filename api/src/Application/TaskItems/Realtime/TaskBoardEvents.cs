
namespace Application.TaskItems.Realtime
{
    public sealed record TaskCreatedPayload(Guid TaskId, Guid ColumnId, Guid LaneId, string Title, string? Description, decimal SortKey);
    public sealed record TaskUpdatedPayload(Guid TaskId, string? NewTitle, string? NewDescription, DateTimeOffset? NewDueDate);
    public sealed record TaskMovedPayload(Guid TaskId, Guid FromLaneId, Guid FromColumnId, Guid ToLaneId, Guid ToColumnId, decimal SortKey);

    public sealed record TaskCreatedEvent(Guid ProjectId, TaskCreatedPayload Payload)
        : Application.Realtime.BoardEvent<TaskCreatedPayload>("task.created", ProjectId, DateTimeOffset.UtcNow, Payload);

    public sealed record TaskUpdatedEvent(Guid ProjectId, TaskUpdatedPayload Payload)
        : Application.Realtime.BoardEvent<TaskUpdatedPayload>("task.updated", ProjectId, DateTimeOffset.UtcNow, Payload);

    public sealed record TaskMovedEvent(Guid ProjectId, TaskMovedPayload Payload)
        : Application.Realtime.BoardEvent<TaskMovedPayload>("task.moved", ProjectId, DateTimeOffset.UtcNow, Payload);
}
