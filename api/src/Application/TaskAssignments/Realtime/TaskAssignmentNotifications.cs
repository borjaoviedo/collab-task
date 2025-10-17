using MediatR;

namespace Application.TaskAssignments.Realtime
{
    public sealed record TaskAssignmentCreated(Guid ProjectId, TaskAssignmentCreatedPayload Payload) : INotification;
    public sealed record TaskAssignmentUpdated(Guid ProjectId, TaskAssignmentUpdatedPayload Payload) : INotification;
    public sealed record TaskAssignmentRemoved(Guid ProjectId, TaskAssignmentRemovedPayload Payload) : INotification;
}
