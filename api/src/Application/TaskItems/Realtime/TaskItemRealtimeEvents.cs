
namespace Application.TaskItems.Realtime
{
    public sealed record TaskItemCreatedPayload(
        Guid TaskId,
        Guid ColumnId,
        Guid LaneId,
        string Title,
        string? Description,
        decimal SortKey);
    public sealed record TaskItemUpdatedPayload(
        Guid TaskId,
        string? NewTitle,
        string? NewDescription,
        DateTimeOffset? NewDueDate);
    public sealed record TaskItemMovedPayload(
        Guid TaskId,
        Guid FromLaneId,
        Guid FromColumnId,
        Guid ToLaneId,
        Guid ToColumnId,
        decimal SortKey);
    public sealed record TaskItemDeletedPayload(Guid TaskId);

    public sealed record TaskItemCreatedEvent(Guid ProjectId, TaskItemCreatedPayload Payload)
        : Application.Realtime.RealtimeEvent<TaskItemCreatedPayload>(TypeName, ProjectId, DateTimeOffset.UtcNow, Payload)
    {
        public const string TypeName = "task.created";
    }

    public sealed record TaskItemUpdatedEvent(Guid ProjectId, TaskItemUpdatedPayload Payload)
        : Application.Realtime.RealtimeEvent<TaskItemUpdatedPayload>(TypeName, ProjectId, DateTimeOffset.UtcNow, Payload)
    {
        public const string TypeName = "task.updated";
    }

    public sealed record TaskItemMovedEvent(Guid ProjectId, TaskItemMovedPayload Payload)
        : Application.Realtime.RealtimeEvent<TaskItemMovedPayload>(TypeName, ProjectId, DateTimeOffset.UtcNow, Payload)
    {
        public const string TypeName = "task.moved";
    }

    public sealed record TaskItemDeletedEvent(Guid ProjectId, TaskItemDeletedPayload Payload)
        : Application.Realtime.RealtimeEvent<TaskItemDeletedPayload>(TypeName, ProjectId, DateTimeOffset.UtcNow, Payload)
    {
        public const string TypeName = "task.deleted";
    }
}
