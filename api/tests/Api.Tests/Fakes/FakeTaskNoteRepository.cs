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

        public Task<IReadOnlyList<TaskNote>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TaskNote>>(_notes.Values.Where(n => n.TaskId == taskId)
                .OrderBy(n => n.CreatedAt).Select(Clone).ToList());

        public Task<IReadOnlyList<TaskNote>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TaskNote>>(_notes.Values.Where(n => n.UserId == userId).Select(Clone).ToList());

        public Task<TaskNote?> GetByIdAsync(Guid noteId, CancellationToken ct = default)
            => Task.FromResult(_notes.TryGetValue(noteId, out var n) ? Clone(n) : null);

        public Task<TaskNote?> GetTrackedByIdAsync(Guid noteId, CancellationToken ct = default)
            => Task.FromResult(_notes.TryGetValue(noteId, out var n) ? n : null);

        public Task AddAsync(TaskNote note, CancellationToken ct = default)
        {
            note.SetRowVersion(NextRowVersion());
            _notes[note.Id] = note;
            return Task.CompletedTask;
        }

        public async Task<PrecheckStatus> EditAsync(Guid noteId, NoteContent newContent, byte[] rowVersion, CancellationToken ct = default)
        {
            var note = await GetTrackedByIdAsync(noteId, ct);
            if (note is null) return PrecheckStatus.NotFound;

            if (string.IsNullOrWhiteSpace(newContent)) return PrecheckStatus.NoOp;
            if (string.Equals(note.Content.Value, newContent.Value, StringComparison.Ordinal)) return PrecheckStatus.NoOp;

            if (!note.RowVersion.SequenceEqual(rowVersion)) return PrecheckStatus.Conflict;

            note.Edit(newContent);
            note.SetRowVersion(NextRowVersion());
            return PrecheckStatus.Ready;
        }

        public async Task<PrecheckStatus> DeleteAsync(Guid noteId, byte[] rowVersion, CancellationToken ct = default)
        {
            var note = await GetTrackedByIdAsync(noteId, ct);
            if (note is null) return PrecheckStatus.NotFound;
            if (!note.RowVersion.SequenceEqual(rowVersion)) return PrecheckStatus.Conflict;

            _notes.Remove(noteId);
            return PrecheckStatus.Ready;
        }

        private static TaskNote Clone(TaskNote n)
        {
            var clone = TaskNote.Create(n.TaskId, n.UserId, NoteContent.Create(n.Content));
            var rowVersion = (n.RowVersion is null) ? [] : n.RowVersion;
            clone.SetRowVersion(rowVersion);
            return clone;
        }
    }
}
