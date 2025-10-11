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

        public async Task<TaskNote?> GetByIdAsync(Guid noteId, CancellationToken ct = default)
            => await _db.TaskNotes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(n => n.Id == noteId, ct);

        public async Task<TaskNote?> GetTrackedByIdAsync(Guid noteId, CancellationToken ct = default)
            => await _db.TaskNotes.FirstOrDefaultAsync(n => n.Id == noteId, ct);

        public async Task<IReadOnlyList<TaskNote>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => await _db.TaskNotes
                        .AsNoTracking()
                        .Where(n => n.TaskId == taskId)
                        .OrderBy(n => n.CreatedAt)
                        .ToListAsync(ct);
        public async Task<IReadOnlyList<TaskNote>> ListByAuthorAsync(Guid authorId, CancellationToken ct = default)
            => await _db.TaskNotes
                        .AsNoTracking()
                        .Where(n => n.AuthorId == authorId)
                        .ToListAsync(ct);

        public async Task AddAsync(TaskNote note, CancellationToken ct = default)
            => await _db.TaskNotes.AddAsync(note, ct);

        public async Task<DomainMutation> EditAsync(Guid noteId, string newContent, byte[] rowVersion, CancellationToken ct = default)
        {
            var note = await GetTrackedByIdAsync(noteId, ct);
            if (note is null) return DomainMutation.NotFound;

            if (string.IsNullOrWhiteSpace(newContent)) return DomainMutation.NoOp;
            var trimmed = newContent.Trim();

            if (string.Equals(note.Content.Value, trimmed, StringComparison.Ordinal))
                return DomainMutation.NoOp;

            _db.Entry(note).Property(t => t.RowVersion).OriginalValue = rowVersion;

            note.Edit(NoteContent.Create(trimmed));
            _db.Entry(note).Property(p => p.Content).IsModified = true;

            try
            {
                await _db.SaveChangesAsync(ct);
                return DomainMutation.Updated;
            }
            catch (DbUpdateConcurrencyException)
            {
                return DomainMutation.Conflict;
            }
        }

        public async Task<DomainMutation> DeleteAsync(Guid noteId, byte[] rowVersion, CancellationToken ct = default)
        {
            var note = await GetTrackedByIdAsync(noteId, ct);
            if (note is null) return DomainMutation.NotFound;

            _db.Entry(note).Property(t => t.RowVersion).OriginalValue = rowVersion;
            _db.TaskNotes.Remove(note);

            try
            {
                await _db.SaveChangesAsync(ct);
                return DomainMutation.Deleted;
            }
            catch (DbUpdateConcurrencyException)
            {
                return DomainMutation.Conflict;
            }
            catch (DbUpdateException)
            {
                return DomainMutation.Conflict;
            }
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default) => await _db.SaveChangesAsync(ct);
    }
}
