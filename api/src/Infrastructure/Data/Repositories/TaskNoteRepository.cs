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
        public async Task<IReadOnlyList<TaskNote>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => await _db.TaskNotes
                        .AsNoTracking()
                        .Where(n => n.AuthorId == userId)
                        .ToListAsync(ct);

        public async Task AddAsync(TaskNote note, CancellationToken ct = default)
            => await _db.TaskNotes.AddAsync(note, ct);

        public async Task<DomainMutation> EditAsync(Guid noteId, NoteContent newContent, byte[] rowVersion, CancellationToken ct = default)
        {
            var note = await GetTrackedByIdAsync(noteId, ct);
            if (note is null) return DomainMutation.NotFound;

            if (string.IsNullOrWhiteSpace(newContent)) return DomainMutation.NoOp;

            if (string.Equals(note.Content.Value, newContent.Value, StringComparison.Ordinal))
                return DomainMutation.NoOp;

            _db.Entry(note).Property(t => t.RowVersion).OriginalValue = rowVersion;

            note.Edit(newContent);
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

        public async Task<int> SaveCreateChangesAsync(CancellationToken ct = default) => await _db.SaveChangesAsync(ct);
        public async Task<DomainMutation> SaveUpdateChangesAsync(CancellationToken ct = default)
        {
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
        public async Task<DomainMutation> SaveDeleteChangesAsync(CancellationToken ct = default)
        {
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
    }
}
