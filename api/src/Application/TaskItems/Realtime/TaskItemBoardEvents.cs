
namespace Application.TaskItems.Realtime
{
    public sealed record TaskItemCreatedPayload(Guid TaskId, Guid ColumnId, Guid LaneId, string Title, string? Description, decimal SortKey);
    public sealed record TaskItemUpdatedPayload(Guid TaskId, string? NewTitle, string? NewDescription, DateTimeOffset? NewDueDate);
    public sealed record TaskItemMovedPayload(Guid TaskId, Guid FromLaneId, Guid FromColumnId, Guid ToLaneId, Guid ToColumnId, decimal SortKey);
    public sealed record TaskItemDeletedPayload(Guid TaskId);

    public sealed record TaskItemCreatedEvent(Guid ProjectId, TaskItemCreatedPayload Payload)
        : Application.Realtime.BoardEvent<TaskItemCreatedPayload>("task.created", ProjectId, DateTimeOffset.UtcNow, Payload);

    public sealed record TaskItemUpdatedEvent(Guid ProjectId, TaskItemUpdatedPayload Payload)
        : Application.Realtime.BoardEvent<TaskItemUpdatedPayload>("task.updated", ProjectId, DateTimeOffset.UtcNow, Payload);

    public sealed record TaskItemMovedEvent(Guid ProjectId, TaskItemMovedPayload Payload)
        : Application.Realtime.BoardEvent<TaskItemMovedPayload>("task.moved", ProjectId, DateTimeOffset.UtcNow, Payload);

    public sealed record TaskItemDeletedEvent(Guid ProjectId, TaskItemDeletedPayload Payload)
        : Application.Realtime.BoardEvent<TaskItemDeletedPayload>("task.deleted", ProjectId, DateTimeOffset.UtcNow, Payload);
}
