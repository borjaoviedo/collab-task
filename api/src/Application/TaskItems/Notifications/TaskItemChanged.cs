using MediatR;

namespace Application.TaskItems.Notifications
{
    public sealed record TaskItemChanged(Guid ProjectId, Guid TaskId, string ChangeType, object Data) : INotification;
}
