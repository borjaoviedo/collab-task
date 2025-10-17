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
                new TaskNoteItemCreated(
                    projectId,
                    new TaskNoteCreatedPayload(taskId, note.Id, note.Content.Value, authorId, note.CreatedAt)
                ),
                ct);
            return (DomainMutation.Created, note);
        }

        public async Task<DomainMutation> EditAsync(
            Guid noteId, Guid userId, string content, byte[] rowVersion, CancellationToken ct = default)
        {
            var mutation = await repo.EditAsync(noteId, content, rowVersion, ct);
            if (mutation != DomainMutation.Updated) return mutation;

            var note = await repo.GetByIdAsync(noteId, ct);
            var payload = ActivityPayloadFactory.NoteEdited(noteId);

            await activityWriter.CreateAsync(note!.TaskId, userId, TaskActivityType.NoteEdited, payload, ct);
            return await repo.SaveUpdateChangesAsync(ct);
        }

        public async Task<DomainMutation> DeleteAsync(Guid noteId, Guid userId, byte[] rowVersion, CancellationToken ct = default)
        {
            var note = await repo.GetByIdAsync(noteId, ct);

            var mutation = await repo.DeleteAsync(noteId, rowVersion, ct);
            if (mutation != DomainMutation.Deleted) return mutation;
            
            var payload = ActivityPayloadFactory.NoteRemoved(noteId);
            await activityWriter.CreateAsync(note!.TaskId, userId, TaskActivityType.NoteRemoved, payload, ct);
            await repo.SaveCreateChangesAsync(ct);

            return DomainMutation.Deleted;
        }
    }
}
