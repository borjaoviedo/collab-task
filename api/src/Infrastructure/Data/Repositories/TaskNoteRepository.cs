using Application.TaskNotes.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories
{
    public sealed class TaskNoteRepository(AppDbContext db) : ITaskNoteRepository
    {
        private readonly AppDbContext _db = db;

        public async Task<IReadOnlyList<TaskNote>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => await _db.TaskNotes
                        .AsNoTracking()
                        .Where(n => n.TaskId == taskId)
                        .OrderBy(n => n.CreatedAt)
                        .ToListAsync(ct);

        public async Task<IReadOnlyList<TaskNote>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => await _db.TaskNotes
                        .AsNoTracking()
                        .Where(n => n.UserId == userId)
                        .ToListAsync(ct);
        public async Task<TaskNote?> GetByIdAsync(Guid noteId, CancellationToken ct = default)
            => await _db.TaskNotes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(n => n.Id == noteId, ct);

        public async Task<TaskNote?> GetTrackedByIdAsync(Guid noteId, CancellationToken ct = default)
            => await _db.TaskNotes.FirstOrDefaultAsync(n => n.Id == noteId, ct);

        public async Task AddAsync(TaskNote note, CancellationToken ct = default)
            => await _db.TaskNotes.AddAsync(note, ct);

        public async Task<PrecheckStatus> EditAsync(Guid noteId, NoteContent newContent, byte[] rowVersion, CancellationToken ct = default)
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
