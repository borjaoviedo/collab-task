using Application.Realtime;
using MediatR;

namespace Application.TaskNotes.Realtime
{
    /// <summary>
    /// Handles domain notifications related to task notes and propagates them as real-time events.
    /// </summary>
    public sealed class TaskNoteChangedHandler(IRealtimeNotifier notifier) :
        INotificationHandler<TaskNoteCreated>,
        INotificationHandler<TaskNoteUpdated>,
        INotificationHandler<TaskNoteDeleted>
    {
        /// <summary>Broadcasts a <c>note.created</c> event when a note is created.</summary>
        public Task Handle(TaskNoteCreated n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(
                n.ProjectId,
                new TaskNoteCreatedEvent(n.ProjectId, n.Payload),
                cancellationToken);

        /// <summary>Broadcasts a <c>note.updated</c> event when a note is updated.</summary>
        public Task Handle(TaskNoteUpdated n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(
                n.ProjectId,
                new TaskNoteUpdatedEvent(n.ProjectId, n.Payload),
                cancellationToken);

        /// <summary>Broadcasts a <c>note.deleted</c> event when a note is deleted.</summary>
        public Task Handle(TaskNoteDeleted n, CancellationToken cancellationToken)
            => notifier.NotifyAsync(
                n.ProjectId,
                new TaskNoteDeletedEvent(n.ProjectId, n.Payload),
                cancellationToken);
    }

}
