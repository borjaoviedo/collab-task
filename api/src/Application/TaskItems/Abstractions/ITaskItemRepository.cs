using Domain.Entities;
using Domain.Enums;

namespace Application.TaskItems.Abstractions
{
    public interface ITaskItemRepository
    {
        Task<TaskItem?> GetByIdAsync(Guid taskId, CancellationToken ct = default);
        Task<TaskItem?> GetTrackedByIdAsync(Guid taskId, CancellationToken ct = default);
        Task<bool> ExistsWithTitleAsync(Guid columnId, string title, Guid? excludeTaskId = null, CancellationToken ct = default);
        Task<IReadOnlyList<TaskItem>> ListByColumnAsync(Guid columnId, CancellationToken ct = default);

        Task AddAsync(TaskItem task, CancellationToken ct = default);

        Task<DomainMutation> EditAsync(Guid taskId, string? newTitle, string? newDescription, DateTimeOffset? newDueDate,
                                        byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> MoveAsync(Guid taskId, Guid targetColumnId, Guid targetLaneId, decimal targetSortKey,
                                        byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(Guid taskId, byte[] rowVersion, CancellationToken ct = default);

        Task<decimal> GetNextSortKeyAsync(Guid columnId, CancellationToken ct = default);
        Task RebalanceSortKeysAsync(Guid columnId, CancellationToken ct = default);

        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
