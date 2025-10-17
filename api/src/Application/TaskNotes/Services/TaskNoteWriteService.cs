using Application.TaskActivities;
using Application.TaskActivities.Abstractions;
using Application.TaskNotes.Abstractions;
using Application.TaskNotes.Realtime;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using MediatR;

namespace Application.TaskNotes.Services
{
    public sealed class TaskNoteWriteService(
        ITaskNoteRepository repo, ITaskActivityWriteService activityWriter, IMediator mediator) : ITaskNoteWriteService
    {
        public async Task<(DomainMutation, TaskNote?)> CreateAsync(Guid projectId, Guid taskId, Guid authorId, string content, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(content)) return (DomainMutation.NoOp, null);

            var note = TaskNote.Create(taskId, authorId, NoteContent.Create(content));

            await repo.AddAsync(note, ct);

            var payload = ActivityPayloadFactory.NoteAdded(note.Id);
            await activityWriter.CreateAsync(taskId, authorId, TaskActivityType.NoteAdded, payload, ct);

            await repo.SaveCreateChangesAsync(ct);
            await mediator.Publish(
                new TaskNoteCreated(
                    projectId, new TaskNoteCreatedPayload(taskId, note.Id, note.Content.Value)
                    ), ct);

            return (DomainMutation.Created, note);
        }

        public async Task<DomainMutation> EditAsync(
            Guid projectId, Guid taskId, Guid noteId, Guid userId, string content, byte[] rowVersion, CancellationToken ct = default)
        {
            var mutation = await repo.EditAsync(noteId, content, rowVersion, ct);
            if (mutation != DomainMutation.Updated) return mutation;

            var payload = ActivityPayloadFactory.NoteEdited(noteId);
            await activityWriter.CreateAsync(taskId, userId, TaskActivityType.NoteEdited, payload, ct);

            var saved = await repo.SaveUpdateChangesAsync(ct);
            if (saved == DomainMutation.Updated)
            {
                await mediator.Publish(new TaskNoteUpdated(projectId, new TaskNoteUpdatedPayload(noteId, content)), ct);
            }

            return saved;
        }

        public async Task<DomainMutation> DeleteAsync(Guid projectId, Guid noteId, Guid userId, byte[] rowVersion, CancellationToken ct = default)
        {
            var note = await repo.GetByIdAsync(noteId, ct);
            if (note is null) return DomainMutation.NotFound;

            var mutation = await repo.DeleteAsync(noteId, rowVersion, ct);
            if (mutation != DomainMutation.Deleted) return mutation;
            
            var payload = ActivityPayloadFactory.NoteRemoved(noteId);
            await activityWriter.CreateAsync(note.TaskId, userId, TaskActivityType.NoteRemoved, payload, ct);

            var saved = await repo.SaveDeleteChangesAsync(ct);
            if (saved == DomainMutation.Deleted)
            {
                await mediator.Publish(new TaskNoteDeleted(projectId, new TaskNoteDeletedPayload(note.Id)), ct);
            }

            return saved;
        }
    }
}
