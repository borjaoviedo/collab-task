
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

    /// <summary>Event emitted when a task is created.</summary>
    public sealed record TaskItemCreatedEvent(Guid ProjectId, TaskItemCreatedPayload Payload)
        : Application.Realtime.RealtimeEvent<TaskItemCreatedPayload>(
            TypeName,
            ProjectId,
            DateTimeOffset.UtcNow,
            Payload)
    {
        public const string TypeName = "task.created";
    }

    /// <summary>Event emitted when a task is updated.</summary>
    public sealed record TaskItemUpdatedEvent(Guid ProjectId, TaskItemUpdatedPayload Payload)
        : Application.Realtime.RealtimeEvent<TaskItemUpdatedPayload>(
            TypeName,
            ProjectId,
            DateTimeOffset.UtcNow,
            Payload)
    {
        public const string TypeName = "task.updated";
    }

    /// <summary>Event emitted when a task is moved between columns or lanes.</summary>
    public sealed record TaskItemMovedEvent(Guid ProjectId, TaskItemMovedPayload Payload)
        : Application.Realtime.RealtimeEvent<TaskItemMovedPayload>(
            TypeName,
            ProjectId,
            DateTimeOffset.UtcNow,
            Payload)
    {
        public const string TypeName = "task.moved";
    }

    /// <summary>Event emitted when a task is deleted.</summary>
    public sealed record TaskItemDeletedEvent(Guid ProjectId, TaskItemDeletedPayload Payload)
        : Application.Realtime.RealtimeEvent<TaskItemDeletedPayload>(
            TypeName,
            ProjectId,
            DateTimeOffset.UtcNow,
            Payload)
    {
        public const string TypeName = "task.deleted";
    }
}
