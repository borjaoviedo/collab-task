using MediatR;

namespace Application.TaskItems.Realtime
{
    public sealed record TaskItemCreated(Guid ProjectId, TaskCreatedPayload Payload) : INotification;
    public sealed record TaskItemUpdated(Guid ProjectId, TaskUpdatedPayload Payload) : INotification;
    public sealed record TaskItemMoved(Guid ProjectId, TaskMovedPayload Payload) : INotification;
}
