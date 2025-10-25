using Application.TaskItems.Changes;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskItems.Abstractions
{
    public interface ITaskItemRepository
    {
        Task<TaskItem?> GetByIdAsync(Guid taskId, CancellationToken ct = default);
        Task<TaskItem?> GetTrackedByIdAsync(Guid taskId, CancellationToken ct = default);
        Task<bool> ExistsWithTitleAsync(
            Guid columnId,
            TaskTitle title,
            Guid? excludeTaskId = null,
            CancellationToken ct = default);
        Task<IReadOnlyList<TaskItem>> ListByColumnAsync(Guid columnId, CancellationToken ct = default);

        Task AddAsync(TaskItem task, CancellationToken ct = default);

        Task<(DomainMutation Mutation, TaskItemChange? Change)> EditAsync(
            Guid taskId,
            TaskTitle? newTitle,
            TaskDescription? newDescription,
            DateTimeOffset? newDueDate,
            byte[] rowVersion,
            CancellationToken ct = default);
        Task<(DomainMutation Mutation, TaskItemChange? Change)> MoveAsync(
            Guid taskId,
            Guid targetColumnId,
            Guid targetLaneId,
            Guid targetProjectId,
            decimal targetSortKey,
            byte[] rowVersion,
            CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(Guid taskId, byte[] rowVersion, CancellationToken ct = default);

        Task<decimal> GetNextSortKeyAsync(Guid columnId, CancellationToken ct = default);
        Task RebalanceSortKeysAsync(Guid columnId, CancellationToken ct = default);

        Task<int> SaveCreateChangesAsync(CancellationToken ct = default);
        Task<DomainMutation> SaveUpdateChangesAsync(CancellationToken ct = default);
    }
}
