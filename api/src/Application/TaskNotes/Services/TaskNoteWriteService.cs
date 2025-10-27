using Application.Common.Abstractions.Extensions;
using Application.Common.Abstractions.Persistence;
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
    public sealed class TaskNoteWriteService(
        ITaskNoteRepository repo,
        IUnitOfWork uow,
        ITaskActivityWriteService activityWriter,
        IMediator mediator) : ITaskNoteWriteService
    {
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
                    new TaskNoteUpdatedPayload(noteId, content));
                await mediator.Publish(notification, ct);
            }

            return saveUpdateResult;
        }

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
                var notification = new TaskNoteDeleted(projectId, new TaskNoteDeletedPayload(note.Id));
                await mediator.Publish(notification, ct);
            }

            return saveDeleteResult;
        }
    }
}
