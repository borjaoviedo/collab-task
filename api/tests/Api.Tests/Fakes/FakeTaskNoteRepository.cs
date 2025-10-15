using Application.TaskNotes.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Api.Tests.Fakes
{
    public sealed class FakeTaskNoteRepository : ITaskNoteRepository
    {
        private readonly Dictionary<Guid, TaskNote> _notes = [];

        private static byte[] NextRowVersion() => Guid.NewGuid().ToByteArray();

        public Task<TaskNote?> GetByIdAsync(Guid noteId, CancellationToken ct = default)
            => Task.FromResult(_notes.TryGetValue(noteId, out var n) ? Clone(n) : null);

        public Task<TaskNote?> GetTrackedByIdAsync(Guid noteId, CancellationToken ct = default)
            => Task.FromResult(_notes.TryGetValue(noteId, out var n) ? n : null);

        public Task<IReadOnlyList<TaskNote>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TaskNote>>(_notes.Values.Where(n => n.TaskId == taskId)
                .OrderBy(n => n.CreatedAt).Select(Clone).ToList());

        public Task<IReadOnlyList<TaskNote>> ListByAuthorAsync(Guid authorId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TaskNote>>(_notes.Values.Where(n => n.AuthorId == authorId).Select(Clone).ToList());

        public Task AddAsync(TaskNote note, CancellationToken ct = default)
        {
            note.RowVersion = NextRowVersion();
            _notes[note.Id] = note;
            return Task.CompletedTask;
        }

        public async Task<DomainMutation> EditAsync(Guid noteId, string newContent, byte[] rowVersion, CancellationToken ct = default)
        {
            var note = await GetTrackedByIdAsync(noteId, ct);
            if (note is null) return DomainMutation.NotFound;

            var trimmed = newContent?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed)) return DomainMutation.NoOp;
            if (string.Equals(note.Content.Value, trimmed, StringComparison.Ordinal)) return DomainMutation.NoOp;

            if (!note.RowVersion.SequenceEqual(rowVersion)) return DomainMutation.Conflict;

            note.Edit(NoteContent.Create(trimmed));
            note.RowVersion = NextRowVersion();
            return DomainMutation.Updated;
        }

        public async Task<DomainMutation> DeleteAsync(Guid noteId, byte[] rowVersion, CancellationToken ct = default)
        {
            var note = await GetTrackedByIdAsync(noteId, ct);
            if (note is null) return DomainMutation.NotFound;
            if (!note.RowVersion.SequenceEqual(rowVersion)) return DomainMutation.Conflict;

            _notes.Remove(noteId);
            return DomainMutation.Deleted;
        }

        public Task<int> SaveCreateChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task<DomainMutation> SaveUpdateChangesAsync(CancellationToken ct = default) => Task.FromResult(DomainMutation.Updated);

        private static TaskNote Clone(TaskNote n)
        {
            var clone = TaskNote.Create(n.TaskId, n.AuthorId, NoteContent.Create(n.Content));
            clone.RowVersion = (n.RowVersion is null) ? Array.Empty<byte>() : n.RowVersion.ToArray();
            return clone;
        }
    }
}
