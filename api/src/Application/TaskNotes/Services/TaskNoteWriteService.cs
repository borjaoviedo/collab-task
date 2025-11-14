using Application.Abstractions.Extensions;
using Application.Abstractions.Persistence;
using Application.TaskActivities.Abstractions;
using Application.TaskActivities.Payloads;
using Application.TaskNotes.Abstractions;
using Application.TaskNotes.Realtime;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using MediatR;

namespace Application.TaskNotes.Services
{
    /// <summary>
    /// Write-side application service for task notes.
    /// </summary>
    public sealed class TaskNoteWriteService(
        ITaskNoteRepository repo,
        IUnitOfWork uow,
        ITaskActivityWriteService activityWriter,
        IMediator mediator) : ITaskNoteWriteService
    {
        /// <summary>
        /// Creates a new note, records a "note added" activity, and publishes a creation notification.
        /// </summary>
        /// <param name="projectId">Project identifier for notification scoping.</param>
        /// <param name="taskId">Task to attach the note to.</param>
        /// <param name="userId">Author user identifier.</param>
        /// <param name="content">Note content.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The mutation result and the created note when successful.</returns>
        public async Task<(DomainMutation, TaskNote?)> CreateAsync(
            Guid projectId,
            Guid taskId,
            Guid userId,
            NoteContent content,
            CancellationToken ct = default)
        {
            var note = TaskNote.Create(taskId, userId, content);
            await repo.AddAsync(note, ct);

            var payload = ActivityPayloadFactory.NoteAdded(note.Id);
            await activityWriter.CreateAsync(
                taskId,
                userId,
                TaskActivityType.NoteAdded,
                payload,
                ct);

            var saveCreateResult = await uow.SaveAsync(MutationKind.Create, ct);

            if (saveCreateResult == DomainMutation.Created)
            {
                var notification = new TaskNoteCreated(
                projectId,
                new TaskNoteCreatedPayload(taskId, note.Id, note.Content.Value));
                await mediator.Publish(notification, ct);
            }

            return (saveCreateResult, note);
        }

        /// <summary>
        /// Edits an existing note, records an edit activity, and publishes an update notification.
        /// </summary>
        /// <param name="projectId">Project identifier for notification scoping.</param>
        /// <param name="taskId">Task identifier.</param>
        /// <param name="noteId">Note identifier.</param>
        /// <param name="userId">Actor user identifier.</param>
        /// <param name="content">New content.</param>
        /// <param name="rowVersion">Concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A mutation describing the outcome.</returns>
        public async Task<DomainMutation> EditAsync(
            Guid projectId,
            Guid taskId,
            Guid noteId,
            Guid userId,
            NoteContent content,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var editStatus = await repo.EditAsync(noteId, content, rowVersion, ct);
            if (editStatus != PrecheckStatus.Ready) return editStatus.ToErrorDomainMutation();

            var payload = ActivityPayloadFactory.NoteEdited(noteId);
            await activityWriter.CreateAsync(
                taskId,
                userId,
                TaskActivityType.NoteEdited,
                payload,
                ct);

            var saveUpdateResult = await uow.SaveAsync(MutationKind.Update, ct);

            if (saveUpdateResult == DomainMutation.Updated)
            {
                var notification = new TaskNoteUpdated(
                    projectId,
                    new TaskNoteUpdatedPayload(taskId, noteId, content.Value));
                await mediator.Publish(notification, ct);
            }

            return saveUpdateResult;
        }

        /// <summary>
        /// Deletes a note, records a removal activity, and publishes a deletion notification.
        /// </summary>
        /// <param name="projectId">Project identifier for notification scoping.</param>
        /// <param name="noteId">Note identifier.</param>
        /// <param name="userId">Actor user identifier.</param>
        /// <param name="rowVersion">Concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A mutation describing the outcome.</returns>
        public async Task<DomainMutation> DeleteAsync(
            Guid projectId,
            Guid noteId,
            Guid userId,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var note = await repo.GetByIdAsync(noteId, ct);
            if (note is null) return DomainMutation.NotFound;

            var deleteStatus = await repo.DeleteAsync(noteId, rowVersion, ct);
            if (deleteStatus != PrecheckStatus.Ready) return deleteStatus.ToErrorDomainMutation();

            var payload = ActivityPayloadFactory.NoteRemoved(noteId);
            await activityWriter.CreateAsync(
                note.TaskId,
                userId,
                TaskActivityType.NoteRemoved,
                payload,
                ct);

            var saveDeleteResult = await uow.SaveAsync(MutationKind.Delete, ct);

            if (saveDeleteResult == DomainMutation.Deleted)
            {
                var notification = new TaskNoteDeleted(
                    projectId,
                    new TaskNoteDeletedPayload(note.TaskId, note.Id));
                await mediator.Publish(notification, ct);
            }

            return saveDeleteResult;
        }
    }

}
