using Application.TaskItems.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskItems.Services
{
    public sealed class TaskItemWriteService(ITaskItemRepository repo) : ITaskItemWriteService
    {
        public async Task<(DomainMutation, TaskItem?)> CreateAsync(Guid projectId, Guid laneId, Guid columnId, string title,
            string description, DateTimeOffset? dueDate = null, decimal? sortKey = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(title)) return (DomainMutation.NoOp, null);

            if (await repo.ExistsWithTitleAsync(columnId, title, ct: ct))
                return (DomainMutation.Conflict, null);

            var key = sortKey ?? await repo.GetNextSortKeyAsync(columnId, ct);
            var task = TaskItem.Create(columnId, laneId, projectId, TaskTitle.Create(title), TaskDescription.Create(description), dueDate, key);

            await repo.AddAsync(task, ct);
            await repo.SaveChangesAsync(ct);
            return (DomainMutation.Created, task);
        }

        public Task<DomainMutation> EditAsync(Guid taskId, string? newTitle, string? newDescription, DateTimeOffset? newDueDate,
            byte[] rowVersion, CancellationToken ct = default)
            => repo.EditAsync(taskId, newTitle, newDescription, newDueDate, rowVersion, ct);

        public Task<DomainMutation> MoveAsync(Guid taskId, Guid targetColumnId, Guid targetLaneId, decimal targetSortKey,
            byte[] rowVersion, CancellationToken ct = default)
            => repo.MoveAsync(taskId, targetColumnId, targetLaneId, targetSortKey, rowVersion, ct);

        public Task<DomainMutation> DeleteAsync(Guid taskId, byte[] rowVersion, CancellationToken ct = default)
            => repo.DeleteAsync(taskId, rowVersion, ct);
    }
}
