using MediatR;

namespace Application.TaskItems.Realtime
{
    public sealed record TaskItemCreated(Guid ProjectId, TaskItemCreatedPayload Payload) : INotification;
    public sealed record TaskItemUpdated(Guid ProjectId, TaskItemUpdatedPayload Payload) : INotification;
    public sealed record TaskItemMoved(Guid ProjectId, TaskItemMovedPayload Payload) : INotification;
}
