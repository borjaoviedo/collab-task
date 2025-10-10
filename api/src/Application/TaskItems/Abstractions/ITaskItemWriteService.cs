using Domain.Entities;
using Domain.Enums;

namespace Application.TaskItems.Abstractions
{
    public interface ITaskItemWriteService
    {
        Task<(DomainMutation, TaskItem?)> CreateAsync(Guid projectId, Guid laneId, Guid columnId, string title, string description,
            DateTimeOffset? dueDate = null, decimal? sortKey = null, CancellationToken ct = default);

        Task<DomainMutation> EditAsync(Guid taskId, string? newTitle, string? newDescription, DateTimeOffset? newDueDate,
            byte[] rowVersion, CancellationToken ct = default);

        Task<DomainMutation> MoveAsync(Guid taskId, Guid targetColumnId, Guid targetLaneId, decimal targetSortKey,
            byte[] rowVersion, CancellationToken ct = default);

        Task<DomainMutation> DeleteAsync(Guid taskId, byte[] rowVersion, CancellationToken ct = default);
    }
}
