using Domain.Enums;

namespace Application.TaskAssignments.Realtime
{
    public sealed record TaskAssignmentCreatedPayload(Guid TaskId, Guid UserId, TaskRole Role);
    public sealed record TaskAssignmentUpdatedPayload(Guid TaskId, Guid UserId, TaskRole NewRole);
    public sealed record TaskAssignmentRemovedPayload(Guid TaskId, Guid UserId);

    public sealed record TaskAssignmentCreatedEvent(Guid ProjectId, TaskAssignmentCreatedPayload Payload)
        : Application.Realtime.RealtimeEvent<TaskAssignmentCreatedPayload>(TypeName, ProjectId, DateTimeOffset.UtcNow, Payload)
    {
        public const string TypeName = "assignment.created";
    }

    public sealed record TaskAssignmentUpdatedEvent(Guid ProjectId, TaskAssignmentUpdatedPayload Payload)
        : Application.Realtime.RealtimeEvent<TaskAssignmentUpdatedPayload>(TypeName, ProjectId, DateTimeOffset.UtcNow, Payload)
    {
        public const string TypeName = "assignment.updated";
    }

    public sealed record TaskAssignmentRemovedEvent(Guid ProjectId, TaskAssignmentRemovedPayload Payload)
        : Application.Realtime.RealtimeEvent<TaskAssignmentRemovedPayload>(TypeName, ProjectId, DateTimeOffset.UtcNow, Payload)
    {
        public const string TypeName = "assignment.removed";
    }
}
