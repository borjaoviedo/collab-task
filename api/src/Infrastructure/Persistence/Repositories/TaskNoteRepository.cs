using Application.TaskNotes.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core repository for <see cref="TaskNote"/> entities.
    /// Supports listing by task or by user, tracked fetch, edit with concurrency,
    /// and deletion with concurrency.
    /// </summary>
    public sealed class TaskNoteRepository(CollabTaskDbContext db) : ITaskNoteRepository
    {
        private readonly CollabTaskDbContext _db = db;

        /// <summary>
        /// Lists notes for a task ordered by creation time.
        /// </summary>
        public async Task<IReadOnlyList<TaskNote>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => await _db.TaskNotes
                        .AsNoTracking()
                        .Where(n => n.TaskId == taskId)
                        .OrderBy(n => n.CreatedAt)
                        .ToListAsync(ct);

        /// <summary>
        /// Lists notes authored by a specific user.
        /// </summary>
        public async Task<IReadOnlyList<TaskNote>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => await _db.TaskNotes
                        .AsNoTracking()
                        .Where(n => n.UserId == userId)
                        .ToListAsync(ct);

        /// <summary>Gets a note by id without tracking.</summary>
        public async Task<TaskNote?> GetByIdAsync(Guid noteId, CancellationToken ct = default)
            => await _db.TaskNotes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(n => n.Id == noteId, ct);

        /// <summary>Gets a note by id with tracking.</summary>
        public async Task<TaskNote?> GetTrackedByIdAsync(Guid noteId, CancellationToken ct = default)
            => await _db.TaskNotes.FirstOrDefaultAsync(n => n.Id == noteId, ct);

        /// <summary>Adds a new note to the context.</summary>
        public async Task AddAsync(TaskNote note, CancellationToken ct = default)
            => await _db.TaskNotes.AddAsync(note, ct);

        /// <summary>
        /// Edits note content with optimistic concurrency and no-op detection.
        /// </summary>
        public async Task<PrecheckStatus> EditAsync(
            Guid noteId,
            NoteContent newContent,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var note = await GetTrackedByIdAsync(noteId, ct);
            if (note is null) return PrecheckStatus.NotFound;

            if (string.IsNullOrWhiteSpace(newContent)) return PrecheckStatus.NoOp;

            if (string.Equals(note.Content.Value, newContent.Value, StringComparison.Ordinal))
                return PrecheckStatus.NoOp;

            _db.Entry(note).Property(t => t.RowVersion).OriginalValue = rowVersion;

            note.Edit(newContent);
            _db.Entry(note).Property(p => p.Content).IsModified = true;

            return PrecheckStatus.Ready;
        }

        /// <summary>
        /// Deletes a note with optimistic concurrency.
        /// </summary>
        public async Task<PrecheckStatus> DeleteAsync(Guid noteId, byte[] rowVersion, CancellationToken ct = default)
        {
            var note = await GetTrackedByIdAsync(noteId, ct);
            if (note is null) return PrecheckStatus.NotFound;

            _db.Entry(note).Property(t => t.RowVersion).OriginalValue = rowVersion;
            _db.TaskNotes.Remove(note);

            return PrecheckStatus.Ready;
        }
    }
}
