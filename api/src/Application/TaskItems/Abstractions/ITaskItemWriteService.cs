using Domain.Entities;
using Domain.Enums;

namespace Application.TaskItems.Abstractions
{
    public interface ITaskItemWriteService
    {
        Task<(DomainMutation, TaskItem?)> CreateAsync(Guid projectId, Guid laneId, Guid columnId, Guid userId,
            string title, string description, DateTimeOffset? dueDate = null, decimal? sortKey = null, CancellationToken ct = default);

        Task<DomainMutation> EditAsync(Guid projectId, Guid taskId, Guid userId, string? newTitle, string? newDescription,
            DateTimeOffset? newDueDate, byte[] rowVersion, CancellationToken ct = default);

        Task<DomainMutation> MoveAsync(Guid projectId, Guid taskId, Guid targetColumnId, Guid targetLaneId, Guid userId,
            decimal targetSortKey, byte[] rowVersion, CancellationToken ct = default);

        Task<DomainMutation> DeleteAsync(Guid projectId, Guid taskId, byte[] rowVersion, CancellationToken ct = default);
    }
}
