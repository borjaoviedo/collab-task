using Application.TaskItems.Abstractions;
using Application.TaskItems.Changes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace TestHelpers.Api.Fakes
{
    public sealed class FakeTaskItemRepository : ITaskItemRepository
    {
        private readonly Dictionary<Guid, TaskItem> _tasks = [];
        private long _rv = 1;

        private byte[] NextRowVersion()
            => BitConverter.GetBytes(Interlocked.Increment(ref _rv));

        public Task<IReadOnlyList<TaskItem>> ListByColumnAsync(Guid columnId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TaskItem>>(_tasks.Values.Where(t => t.ColumnId == columnId)
                .OrderBy(t => t.SortKey).Select(Clone).ToList());

        public Task<TaskItem?> GetByIdAsync(Guid taskId, CancellationToken ct = default)
            => Task.FromResult(_tasks.TryGetValue(taskId, out var t) ? Clone(t) : null);

        public Task<TaskItem?> GetTrackedByIdAsync(Guid taskId, CancellationToken ct = default)
            => Task.FromResult(_tasks.TryGetValue(taskId, out var t) ? t : null);

        public Task AddAsync(TaskItem task, CancellationToken ct = default)
        {
            task.SetRowVersion(NextRowVersion());
            _tasks[task.Id] = task;
            return Task.CompletedTask;
        }

        public async Task<(PrecheckStatus Status, TaskItemEditedChange? Change)> EditAsync(
            Guid taskId,
            TaskTitle? newTitle,
            TaskDescription? newDescription,
            DateTimeOffset? newDueDate,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var task = await GetTrackedByIdAsync(taskId, ct);

            if (task is null) return (PrecheckStatus.NotFound, null);
            if (!task.RowVersion.SequenceEqual(rowVersion)) return (PrecheckStatus.Conflict, null);
            if (newTitle != null && await ExistsWithTitleAsync(task.ColumnId, newTitle, task.Id, ct))
                return (PrecheckStatus.Conflict, null);

            var beforeTitle = task.Title;
            var beforeDesc = task.Description;
            var beforeDue = task.DueDate;

            task.Edit(newTitle, newDescription, newDueDate);

            if (Equals(beforeTitle, task.Title)
                && Equals(beforeDesc, task.Description)
                && Nullable.Equals(beforeDue, task.DueDate))
                return (PrecheckStatus.NoOp, null);

            task.SetRowVersion(NextRowVersion());
            return (PrecheckStatus.Ready, null);
        }

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

            if (task is null) return (PrecheckStatus.NotFound, null);
            if (!task.RowVersion.SequenceEqual(rowVersion)) return (PrecheckStatus.Conflict, null);
            if (task.ColumnId == targetColumnId && task.LaneId == targetLaneId && task.SortKey == targetSortKey)
                return (PrecheckStatus.NoOp, null);

            task.Move(targetProjectId, targetLaneId, targetColumnId, targetSortKey);
            task.SetRowVersion(NextRowVersion());
            return (PrecheckStatus.Ready, null);
        }

        public async Task<PrecheckStatus> DeleteAsync(Guid taskId, byte[] rowVersion, CancellationToken ct = default)
        {
            var task = await GetTrackedByIdAsync(taskId, ct);
            if (task is null) return PrecheckStatus.NotFound;

            _tasks.Remove(taskId);
            return PrecheckStatus.Ready;
        }

        public Task<bool> ExistsWithTitleAsync(
            Guid columnId,
            TaskTitle title,
            Guid? excludeTaskId = null,
            CancellationToken ct = default)
        {
            var q = _tasks.Values.Where(t => t.ColumnId == columnId && t.Title == title);
            if (excludeTaskId is Guid id) q = q.Where(t => t.Id != id);
            return Task.FromResult(q.Any());
        }

        public Task<decimal> GetNextSortKeyAsync(Guid columnId, CancellationToken ct = default)
        {
            var max = _tasks.Values
                            .Where(t => t.ColumnId == columnId)
                            .Select(t => (decimal?)t.SortKey)
                            .DefaultIfEmpty(null)
                            .Max();
            return Task.FromResult((max ?? -1m) + 1m);
        }

        private static TaskItem Clone(TaskItem t)
        {
            var clone = TaskItem.Create(
                t.ColumnId,
                t.LaneId,
                t.ProjectId,
                t.Title,
                t.Description,
                t.DueDate,
                t.SortKey);
            var rowVersion = (t.RowVersion is null) ? [] : t.RowVersion.ToArray();
            clone.SetRowVersion(rowVersion);
            return clone;
        }
    }
}
