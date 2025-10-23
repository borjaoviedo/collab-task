using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskItems.Abstractions
{
    public interface ITaskItemWriteService
    {
        Task<(DomainMutation, TaskItem?)> CreateAsync(
            Guid projectId,
            Guid laneId,
            Guid columnId,
            Guid userId,
            TaskTitle title,
            TaskDescription description,
            DateTimeOffset? dueDate = null,
            decimal? sortKey = null,
            CancellationToken ct = default);

        Task<DomainMutation> EditAsync(
            Guid projectId,
            Guid taskId,
            Guid userId,
            TaskTitle? newTitle,
            TaskDescription? newDescription,
            DateTimeOffset? newDueDate,
            byte[] rowVersion,
            CancellationToken ct = default);

        Task<DomainMutation> MoveAsync(
            Guid projectId,
            Guid taskId,
            Guid targetColumnId,
            Guid targetLaneId,
            Guid userId,
            decimal targetSortKey,
            byte[] rowVersion,
            CancellationToken ct = default);

        Task<DomainMutation> DeleteAsync(
            Guid projectId,
            Guid taskId,
            byte[] rowVersion,
            CancellationToken ct = default);
    }
}
