using Domain.Enums;

namespace Application.TaskAssignments.Realtime
{
    public sealed record TaskAssignmentCreatedPayload(Guid TaskId, Guid UserId, TaskRole Role);
    public sealed record TaskAssignmentUpdatedPayload(Guid TaskId, Guid UserId, TaskRole NewRole);
    public sealed record TaskAssignmentRemovedPayload(Guid TaskId, Guid UserId);

    public sealed record TaskAssignmentCreatedEvent(Guid ProjectId, TaskAssignmentCreatedPayload Payload)
        : Application.Realtime.BoardEvent<TaskAssignmentCreatedPayload>("assignment.created", ProjectId, DateTimeOffset.UtcNow, Payload);

    public sealed record TaskAssignmentUpdatedEvent(Guid ProjectId, TaskAssignmentUpdatedPayload Payload)
        : Application.Realtime.BoardEvent<TaskAssignmentUpdatedPayload>("assignment.updated", ProjectId, DateTimeOffset.UtcNow, Payload);

    public sealed record TaskAssignmentRemovedEvent(Guid ProjectId, TaskAssignmentRemovedPayload Payload)
        : Application.Realtime.BoardEvent<TaskAssignmentRemovedPayload>("assignment.removed", ProjectId, DateTimeOffset.UtcNow, Payload);
}
