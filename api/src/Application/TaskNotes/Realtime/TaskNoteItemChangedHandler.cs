using Application.Realtime;
using MediatR;

namespace Application.TaskNotes.Realtime
{
    public sealed class TaskNoteItemChangedHandler(IBoardNotifier notifier)
    : INotificationHandler<TaskNoteItemCreated>
    {
        public Task Handle(TaskNoteItemCreated n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(n.ProjectId, new TaskNoteCreatedEvent(n.ProjectId, n.Payload), cancellationToken);
    }
}
