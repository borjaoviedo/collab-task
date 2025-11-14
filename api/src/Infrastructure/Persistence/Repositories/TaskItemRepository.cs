using Application.TaskItems.Abstractions;
using Application.TaskItems.Changes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core repository for <see cref="TaskItem"/> aggregates.
    /// Supports listing by column, tracked fetch, creation, edit with field-level change tracking,
    /// move with integrity checks across project/lane/column, and deletion with concurrency.
    /// </summary>
    public sealed class TaskItemRepository(AppDbContext db) : ITaskItemRepository
    {
        private readonly AppDbContext _db = db;

        /// <summary>
        /// Lists tasks for a column ordered by <see cref="TaskItem.SortKey"/>.
        /// </summary>
        public async Task<IReadOnlyList<TaskItem>> ListByColumnAsync(Guid columnId, CancellationToken ct = default)
            => await _db.TaskItems
                        .AsNoTracking()
                        .Where(t => t.ColumnId == columnId)
                        .OrderBy(t => t.SortKey)
                        .ToListAsync(ct);

        /// <summary>Gets a task by id without tracking.</summary>
        public async Task<TaskItem?> GetByIdAsync(Guid taskId, CancellationToken ct = default)
            => await _db.TaskItems
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == taskId, ct);

        /// <summary>Gets a task by id with tracking.</summary>
        public async Task<TaskItem?> GetTrackedByIdAsync(Guid taskId, CancellationToken ct = default)
            => await _db.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId, ct);

        /// <summary>Adds a new task to the context.</summary>
        public async Task AddAsync(TaskItem task, CancellationToken ct = default)
            => await _db.TaskItems.AddAsync(task, ct);

        /// <summary>
        /// Edits task fields with optimistic concurrency and title uniqueness per column.
        /// Returns a change descriptor when something actually changed.
        /// </summary>
        /// <param name="taskId">Target task id.</param>
        /// <param name="newTitle">Optional new title.</param>
        /// <param name="newDescription">Optional new description.</param>
        /// <param name="newDueDate">Optional new due date.</param>
        /// <param name="rowVersion">Original concurrency token.</param>
        /// <returns>
        /// <see cref="PrecheckStatus.Ready"/> with a <see cref="TaskItemEditedChange"/> when updates are pending,
        /// <see cref="PrecheckStatus.NoOp"/> if nothing changed,
        /// <see cref="PrecheckStatus.Conflict"/> on title uniqueness violation,
        /// or <see cref="PrecheckStatus.NotFound"/>.
        /// </returns>
        public async Task<(PrecheckStatus Status, TaskItemEditedChange? Change)> EditAsync(
            Guid taskId,
            TaskTitle? newTitle,
            TaskDescription? newDescription,
            DateTimeOffset? newDueDate,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var task = await GetTrackedByIdAsync(taskId, ct);
            if (task is null) return (PrecheckStatus.NotFound, Change: null);

            _db.Entry(task).Property(t => t.RowVersion).OriginalValue = rowVersion;

            // Enforce per-column title uniqueness.
            if (newTitle != null && await ExistsWithTitleAsync(task.ColumnId, newTitle, task.Id, ct))
                return (PrecheckStatus.Conflict, Change: null);

            var before = (task.Title, task.Description, task.DueDate);
            task.Edit(newTitle, newDescription, newDueDate);

            var changed = false;

            if (!Equals(before.Title, task.Title))
            {
                _db.Entry(task).Property(t => t.Title).IsModified = true;
                changed = true;
            }

            if (!Equals(before.Description, task.Description))
            {
                _db.Entry(task).Property(t => t.Description).IsModified = true;
                changed = true;
            }

            if (!Nullable.Equals(before.DueDate, task.DueDate))
            {
                _db.Entry(task).Property(t => t.DueDate).IsModified = true;
                changed = true;
            }

            if (!changed) return (PrecheckStatus.NoOp, Change: null);

            var taskItemChange = new TaskItemEditedChange(
                before.Title?.Value, task.Title?.Value,
                before.Description?.Value, task.Description?.Value,
                before.DueDate, task.DueDate);

            return (PrecheckStatus.Ready, Change: taskItemChange);
        }

        /// <summary>
        /// Moves a task to a target column/lane with integrity validation and concurrency.
        /// Produces a move change descriptor on success.
        /// </summary>
        /// <param name="taskId">Task id to move.</param>
        /// <param name="targetColumnId">Destination column id.</param>
        /// <param name="targetLaneId">Destination lane id.</param>
        /// <param name="targetProjectId">Destination project id (must match task.ProjectId).</param>
        /// <param name="targetSortKey">Destination sort key.</param>
        /// <param name="rowVersion">Original concurrency token.</param>
        public async Task<(PrecheckStatus Status, TaskItemMovedChange? Change)> MoveAsync(
            Guid taskId,
            Guid targetColumnId,
            Guid targetLaneId,
            Guid targetProjectId,
            decimal targetSortKey,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var task = await GetTrackedByIdAsync(taskId, ct);
            if (task is null) return (PrecheckStatus.NotFound, Change: null);

            _db.Entry(task).Property(t => t.RowVersion).OriginalValue = rowVersion;

            // Load target structures for relationship validation.
            var targetColumn = await _db.Columns
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(c => c.Id == targetColumnId, ct);
            var targetLane = await _db.Lanes
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync(l => l.Id == targetLaneId, ct);
            if (targetColumn is null || targetLane is null) return (PrecheckStatus.NotFound, Change: null);

            // Integrity: column must belong to lane, and both must tie to the task's project.
            if (targetColumn.LaneId != targetLaneId) return (PrecheckStatus.Conflict, Change: null);
            if (targetColumn.ProjectId != task.ProjectId || targetLane.ProjectId != task.ProjectId)
                return (PrecheckStatus.Conflict, Change: null);

            // No-op if location and order are unchanged.
            if (task.ColumnId == targetColumnId && task.LaneId == targetLaneId && task.SortKey == targetSortKey)
                return (PrecheckStatus.NoOp, Change: null);

            var taskItemChange = new TaskItemMovedChange(
                task.LaneId, targetLaneId,
                task.ColumnId, targetColumnId,
                task.SortKey, targetSortKey
            );

            task.Move(targetProjectId, targetLaneId, targetColumnId, targetSortKey);
            _db.Entry(task).Property(t => t.ColumnId).IsModified = true;
            _db.Entry(task).Property(t => t.LaneId).IsModified = true;
            _db.Entry(task).Property(t => t.SortKey).IsModified = true;

            return (PrecheckStatus.Ready, Change: taskItemChange);
        }

        /// <summary>
        /// Deletes a task with optimistic concurrency.
        /// </summary>
        public async Task<PrecheckStatus> DeleteAsync(Guid taskId, byte[] rowVersion, CancellationToken ct = default)
        {
            var task = await GetTrackedByIdAsync(taskId, ct);
            if (task is null) return PrecheckStatus.NotFound;

            _db.Entry(task).Property(t => t.RowVersion).OriginalValue = rowVersion;
            _db.TaskItems.Remove(task);

            return PrecheckStatus.Ready;
        }

        /// <summary>
        /// Checks whether a title already exists within a column, optionally excluding a task id.
        /// </summary>
        public async Task<bool> ExistsWithTitleAsync(
            Guid columnId,
            TaskTitle title,
            Guid? excludeTaskId = null,
            CancellationToken ct = default)
        {
            var q = _db.TaskItems
                        .AsNoTracking()
                        .Where(t => t.ColumnId == columnId && t.Title == title);

            if (excludeTaskId.HasValue)
                q = q.Where(t => t.Id != excludeTaskId.Value);

            return await q.AnyAsync(ct);
        }

        /// <summary>
        /// Computes the next sort key for a column as (max + 1), or 0 when empty.
        /// </summary>
        public async Task<decimal> GetNextSortKeyAsync(Guid columnId, CancellationToken ct = default)
        {
            var max = await _db.TaskItems
                                .AsNoTracking()
                                .Where(t => t.ColumnId == columnId)
                                .Select(t => (decimal?)t.SortKey)
                                .MaxAsync(ct);

            return (max ?? -1m) + 1m;
        }
    }
}
