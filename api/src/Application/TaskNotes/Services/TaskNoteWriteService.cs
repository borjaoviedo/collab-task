using Application.TaskNotes.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskNotes.Services
{
    public sealed class TaskNoteWriteService(ITaskNoteRepository repo) : ITaskNoteWriteService
    {
        public async Task<(DomainMutation, TaskNote?)> CreateAsync(Guid taskId, Guid authorId, string content, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(content)) return (DomainMutation.NoOp, null);

            var note = TaskNote.Create(taskId, authorId, NoteContent.Create(content));

            await repo.AddAsync(note, ct);
            await repo.SaveChangesAsync(ct);
            return (DomainMutation.Created, note);
        }

        public async Task<DomainMutation> EditAsync(Guid noteId, string content, byte[] rowVersion, CancellationToken ct = default)
            => await repo.EditAsync(noteId, content, rowVersion, ct);

        public async Task<DomainMutation> DeleteAsync(Guid noteId, byte[] rowVersion, CancellationToken ct = default)
            => await repo.DeleteAsync(noteId, rowVersion, ct);
    }
}
