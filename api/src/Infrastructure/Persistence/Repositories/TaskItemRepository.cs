using Application.TaskItems.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core repository for <see cref="TaskItem"/> aggregates.
    /// Provides optimized read and write operations for task items,
    /// including ordered listing by column, tracked and untracked retrieval,
    /// title uniqueness checks, and sort-key generation for stable ordering.
    /// Task items represent the core work units of a project board, and this
    /// repository exposes efficient persistence primitives used by the
    /// application layer to support creation, updating, movement, and deletion
    /// of tasks while preserving domain invariants such as unique titles per
    /// column and strictly monotonic ordering.
    /// </summary>
    /// <param name="db">
    /// The <see cref="CollabTaskDbContext"/> used to query and persist
    /// <see cref="TaskItem"/> entities.
    /// </param>
    public sealed class TaskItemRepository(CollabTaskDbContext db) : ITaskItemRepository
    {
        private readonly CollabTaskDbContext _db = db;

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskItem>> ListByColumnIdAsync(Guid columnId, CancellationToken ct = default)
            => await _db.TaskItems
                        .AsNoTracking()
                        .Where(t => t.ColumnId == columnId)
                        .OrderBy(t => t.SortKey)
                        .ToListAsync(ct);

        /// <inheritdoc/>
        public async Task<TaskItem?> GetByIdAsync(Guid taskId, CancellationToken ct = default)
            => await _db.TaskItems
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == taskId, ct);

        /// <inheritdoc/>
        public async Task<TaskItem?> GetByIdForUpdateAsync(Guid taskId, CancellationToken ct = default)
            => await _db.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId, ct);

        /// <inheritdoc/>
        public async Task<bool> ExistsWithTitleAsync(
            Guid columnId,
            string taskTitle,
            Guid? excludeTaskId = null,
            CancellationToken ct = default)
        {
            var q = _db.TaskItems
                        .AsNoTracking()
                        .Where(t => t.ColumnId == columnId && t.Title == taskTitle);

            if (excludeTaskId.HasValue)
                q = q.Where(t => t.Id != excludeTaskId.Value);

            return await q.AnyAsync(ct);
        }

        /// <inheritdoc/>
        public async Task<decimal> GetNextSortKeyAsync(Guid columnId, CancellationToken ct = default)
        {
            var max = await _db.TaskItems
                                .AsNoTracking()
                                .Where(t => t.ColumnId == columnId)
                                .Select(t => (decimal?)t.SortKey)
                                .MaxAsync(ct);

            return (max ?? -1m) + 1m;
        }

        /// <inheritdoc/>
        public async Task AddAsync(TaskItem task, CancellationToken ct = default)
            => await _db.TaskItems.AddAsync(task, ct);

        /// <inheritdoc/>
        public async Task UpdateAsync(TaskItem task, CancellationToken ct = default)
        {
            // If entity is already tracked, do nothing so EF change tracking produces minimal UPDATEs
            if (_db.Entry(task).State == EntityState.Detached)
                _db.TaskItems.Update(task);

            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(TaskItem task, CancellationToken ct = default)
        {
            // Mark entity as deleted; actual deletion occurs in UnitOfWork.SaveAsync()
            _db.TaskItems.Remove(task);
            await Task.CompletedTask;
        }
    }
}
