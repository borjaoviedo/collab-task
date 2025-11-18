using Application.TaskNotes.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core repository for <see cref="TaskNote"/> aggregates.
    /// Provides efficient read and write operations for task notes, including
    /// listing by task or user, tracked and untracked retrieval, and CRUD persistence.
    /// Notes represent user-authored comments attached to tasks and contribute to a task’s
    /// audit trail and collaboration history. Read operations use <c>AsNoTracking()</c>
    /// to maximize performance on immutable note records, while update operations leverage
    /// EF Core change tracking to generate minimal UPDATE statements.
    /// </summary>
    /// <param name="db">
    /// The <see cref="CollabTaskDbContext"/> used to query and persist
    /// <see cref="TaskNote"/> entities.
    /// </param>
    public sealed class TaskNoteRepository(CollabTaskDbContext db) : ITaskNoteRepository
    {
        private readonly CollabTaskDbContext _db = db;

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskNote>> ListByTaskIdAsync(Guid taskId, CancellationToken ct = default)
            => await _db.TaskNotes
                        .AsNoTracking()
                        .Where(n => n.TaskId == taskId)
                        .OrderBy(n => n.CreatedAt)
                        .ToListAsync(ct);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskNote>> ListByUserIdAsync(Guid userId, CancellationToken ct = default)
            => await _db.TaskNotes
                        .AsNoTracking()
                        .Where(n => n.UserId == userId)
                        .ToListAsync(ct);

        /// <inheritdoc/>
        public async Task<TaskNote?> GetByIdAsync(Guid noteId, CancellationToken ct = default)
            => await _db.TaskNotes
                        .AsNoTracking()
                        .FirstOrDefaultAsync(n => n.Id == noteId, ct);

        /// <inheritdoc/>
        public async Task<TaskNote?> GetByIdForUpdateAsync(Guid noteId, CancellationToken ct = default)
            => await _db.TaskNotes.FirstOrDefaultAsync(n => n.Id == noteId, ct);

        /// <inheritdoc/>
        public async Task AddAsync(TaskNote note, CancellationToken ct = default)
            => await _db.TaskNotes.AddAsync(note, ct);

        /// <inheritdoc/>
        public async Task UpdateAsync(TaskNote note, CancellationToken ct = default)
        {
            // If entity is already tracked, do nothing so EF change tracking produces minimal UPDATEs
            if (_db.Entry(note).State == EntityState.Detached)
                _db.TaskNotes.Update(note);

            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(TaskNote note, CancellationToken ct = default)
        {
            // Mark entity as deleted; actual deletion occurs in UnitOfWork.SaveAsync()
            _db.TaskNotes.Remove(note);
            await Task.CompletedTask;
        }
    }
}
