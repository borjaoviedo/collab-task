using Application.Common.Changes;
using Application.TaskItems.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Api.Tests.Fakes
{
    public sealed class FakeTaskItemRepository : ITaskItemRepository
    {
        private readonly Dictionary<Guid, TaskItem> _tasks = [];
        private long _rv = 1;

        private byte[] NextRowVersion()
            => BitConverter.GetBytes(Interlocked.Increment(ref _rv));

        public Task<TaskItem?> GetByIdAsync(Guid taskId, CancellationToken ct = default)
            => Task.FromResult(_tasks.TryGetValue(taskId, out var t) ? Clone(t) : null);

        public Task<TaskItem?> GetTrackedByIdAsync(Guid taskId, CancellationToken ct = default)
            => Task.FromResult(_tasks.TryGetValue(taskId, out var t) ? t : null);

        public Task<bool> ExistsWithTitleAsync(Guid columnId, TaskTitle title, Guid? excludeTaskId = null, CancellationToken ct = default)
        {
            var q = _tasks.Values.Where(t => t.ColumnId == columnId && t.Title == title);
            if (excludeTaskId is Guid id) q = q.Where(t => t.Id != id);
            return Task.FromResult(q.Any());
        }

        public Task<IReadOnlyList<TaskItem>> ListByColumnAsync(Guid columnId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<TaskItem>>(_tasks.Values.Where(t => t.ColumnId == columnId)
                .OrderBy(t => t.SortKey).Select(Clone).ToList());

        public Task AddAsync(TaskItem task, CancellationToken ct = default)
        {
            task.RowVersion = NextRowVersion();
            _tasks[task.Id] = task;
            return Task.CompletedTask;
        }

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
            if (!task.RowVersion.SequenceEqual(rowVersion)) return (DomainMutation.Conflict, null);
            if (newTitle != null && await ExistsWithTitleAsync(task.ColumnId, newTitle, task.Id, ct)) return (DomainMutation.Conflict, null);

            var beforeTitle = task.Title;
            var beforeDesc = task.Description;
            var beforeDue = task.DueDate;

            task.Edit(newTitle, newDescription, newDueDate);

            if (Equals(beforeTitle, task.Title) && Equals(beforeDesc, task.Description) && Nullable.Equals(beforeDue, task.DueDate))
                return (DomainMutation.NoOp, null);

            task.RowVersion = NextRowVersion();
            return (DomainMutation.Updated, null);
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
            if (!task.RowVersion.SequenceEqual(rowVersion)) return (DomainMutation.Conflict, null);
            if (task.ColumnId == targetColumnId && task.LaneId == targetLaneId && task.SortKey == targetSortKey)
                return (DomainMutation.NoOp, null);

            task.Move(targetLaneId, targetColumnId, targetSortKey);
            task.RowVersion = NextRowVersion();
            return (DomainMutation.Updated, null);
        }

        public async Task<DomainMutation> DeleteAsync(Guid taskId, byte[] rowVersion, CancellationToken ct = default)
        {
            var task = await GetTrackedByIdAsync(taskId, ct);
            if (task is null) return DomainMutation.NotFound;
            if (!task.RowVersion.SequenceEqual(rowVersion)) return DomainMutation.Conflict;

            _tasks.Remove(taskId);
            return DomainMutation.Deleted;
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

        public Task RebalanceSortKeysAsync(Guid columnId, CancellationToken ct = default)
        {
            var list = _tasks.Values.Where(t => t.ColumnId == columnId).OrderBy(t => t.SortKey).ToList();
            for (int i = 0; i < list.Count; i++) list[i].SortKey = i;
            return Task.CompletedTask;
        }

        public Task<int> SaveCreateChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task<DomainMutation> SaveUpdateChangesAsync(CancellationToken ct = default) => Task.FromResult(DomainMutation.Updated);

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
            clone.Id = t.Id;
            clone.CreatedAt = t.CreatedAt;
            clone.UpdatedAt = t.UpdatedAt;
            clone.RowVersion = (t.RowVersion is null) ? Array.Empty<byte>() : t.RowVersion.ToArray();
            return clone;
        }
    }
}
