using Application.Common.Changes;
using Application.TaskItems.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories
{
    public sealed class TaskItemRepository(AppDbContext db) : ITaskItemRepository
    {
        private readonly AppDbContext _db = db;

        public async Task<TaskItem?> GetByIdAsync(Guid taskId, CancellationToken ct = default)
            => await _db.TaskItems
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == taskId, ct);
        public async Task<TaskItem?> GetTrackedByIdAsync(Guid taskId, CancellationToken ct = default)
            => await _db.TaskItems.FirstOrDefaultAsync(t => t.Id == taskId, ct);

        public async Task<bool> ExistsWithTitleAsync(Guid columnId, TaskTitle title, Guid? excludeTaskId = null, CancellationToken ct = default)
        {
            var q = _db.TaskItems.AsNoTracking().Where(t => t.ColumnId == columnId && t.Title == title);
            if (excludeTaskId.HasValue) q = q.Where(t => t.Id != excludeTaskId.Value);
            return await q.AnyAsync(ct);
        }

        public async Task<IReadOnlyList<TaskItem>> ListByColumnAsync(Guid columnId, CancellationToken ct = default)
            => await _db.TaskItems
                        .AsNoTracking()
                        .Where(t => t.ColumnId == columnId)
                        .OrderBy(t => t.SortKey)
                        .ToListAsync(ct);

        public async Task AddAsync(TaskItem task, CancellationToken ct = default)
            => await _db.TaskItems.AddAsync(task, ct);

        public async Task<(DomainMutation Mutation, TaskItemChange? Change)> EditAsync(
            Guid taskId,
            TaskTitle? newTitle,
            TaskDescription? newDescription,
            DateTimeOffset? newDueDate,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var task = await GetTrackedByIdAsync(taskId, ct);
            if (task is null) return (DomainMutation.NotFound, null);

            _db.Entry(task).Property(t => t.RowVersion).OriginalValue = rowVersion;

            // Enforce uniqueness
            if (newTitle != null && await ExistsWithTitleAsync(task.ColumnId, newTitle, task.Id, ct))
                return (DomainMutation.Conflict, null);

            var titleBefore = task.Title;
            var descriptionBefore = task.Description;
            var dueDateBefore = task.DueDate;

            task.Edit(newTitle, newDescription, newDueDate);

            var changed = false;
            if (!Equals(titleBefore, task.Title)) { _db.Entry(task).Property(t => t.Title).IsModified = true; changed = true; }
            if (!Equals(descriptionBefore, task.Description)) { _db.Entry(task).Property(t => t.Description).IsModified = true; changed = true; }
            if (!Nullable.Equals(dueDateBefore, task.DueDate)) { _db.Entry(task).Property(t => t.DueDate).IsModified = true; changed = true; }

            if (!changed) return (DomainMutation.NoOp, null);

            var change = new TaskItemEditedChange(
                titleBefore?.Value, task.Title?.Value,
                descriptionBefore?.Value, task.Description?.Value,
                dueDateBefore, task.DueDate);

            return (DomainMutation.Updated, change);
        }

        public async Task<(DomainMutation Mutation, TaskItemChange? Change)> MoveAsync(
            Guid taskId,
            Guid targetColumnId,
            Guid targetLaneId,
            decimal targetSortKey,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var task = await GetTrackedByIdAsync(taskId, ct);
            if (task is null) return (DomainMutation.NotFound, null);

            _db.Entry(task).Property(t => t.RowVersion).OriginalValue = rowVersion;

            // Load target column and lane to validate relationships
            var targetColumn = await _db.Columns.AsNoTracking().FirstOrDefaultAsync(c => c.Id == targetColumnId, ct);
            if (targetColumn is null) return (DomainMutation.NotFound, null);

            var targetLane = await _db.Lanes.AsNoTracking().FirstOrDefaultAsync(l => l.Id == targetLaneId, ct);
            if (targetLane is null) return (DomainMutation.NotFound, null);

            // Integrity: column must belong to lane, and both to same project as task
            if (targetColumn.LaneId != targetLaneId) return (DomainMutation.Conflict, null);
            if (targetColumn.ProjectId != task.ProjectId || targetLane.ProjectId != task.ProjectId) return (DomainMutation.Conflict, null);

            // No-op if same place and same sort key
            if (task.ColumnId == targetColumnId && task.LaneId == targetLaneId && task.SortKey == targetSortKey)
                return (DomainMutation.NoOp, null);

            var change = new TaskItemMovedChange(
                task.LaneId, targetLaneId,
                task.ColumnId, targetColumnId,
                task.SortKey, targetSortKey
            );

            task.Move(targetLaneId, targetColumnId, targetSortKey);
            _db.Entry(task).Property(t => t.ColumnId).IsModified = true;
            _db.Entry(task).Property(t => t.LaneId).IsModified = true;
            _db.Entry(task).Property(t => t.SortKey).IsModified = true;

            return (DomainMutation.Updated, change);
        }

        public async Task<DomainMutation> DeleteAsync(Guid taskId, byte[] rowVersion, CancellationToken ct = default)
        {
            var task = await GetTrackedByIdAsync(taskId, ct);
            if (task is null) return DomainMutation.NotFound;

            _db.Entry(task).Property(t => t.RowVersion).OriginalValue = rowVersion;

            _db.TaskItems.Remove(task);

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

        public async Task<decimal> GetNextSortKeyAsync(Guid columnId, CancellationToken ct = default)
        {
            var max = await _db.TaskItems
                                .Where(t => t.ColumnId == columnId)
                                .Select(t => (decimal?)t.SortKey)
                                .MaxAsync(ct);

            return (max ?? -1m) + 1m;
        }

        public async Task RebalanceSortKeysAsync(Guid columnId, CancellationToken ct = default)
        {
            using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var tasks = await _db.TaskItems
                                    .Where(t => t.ColumnId == columnId)
                                    .OrderBy(t => t.SortKey)
                                    .ToListAsync(ct);

                for (int i = 0; i < tasks.Count; i++)
                {
                    if (tasks[i].SortKey != i)
                    {
                        tasks[i].SortKey = i;
                        _db.Entry(tasks[i]).Property(t => t.SortKey).IsModified = true;
                    }
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
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
    }
}
