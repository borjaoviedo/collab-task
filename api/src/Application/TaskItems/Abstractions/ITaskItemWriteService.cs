using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskItems.Abstractions
{
    /// <summary>
    /// Handles creation and mutation commands for task items at the application level.
    /// </summary>
    public interface ITaskItemWriteService
    {
        /// <summary>Creates a new task within a column and lane.</summary>
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

        /// <summary>Edits the title, description, or due date of an existing task.</summary>
        Task<DomainMutation> EditAsync(
            Guid projectId,
            Guid taskId,
            Guid userId,
            TaskTitle? newTitle,
            TaskDescription? newDescription,
            DateTimeOffset? newDueDate,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Moves a task to another column or lane.</summary>
        Task<DomainMutation> MoveAsync(
            Guid projectId,
            Guid taskId,
            Guid targetColumnId,
            Guid targetLaneId,
            Guid userId,
            decimal targetSortKey,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Deletes an existing task.</summary>
        Task<DomainMutation> DeleteAsync(
            Guid projectId,
            Guid taskId,
            byte[] rowVersion,
            CancellationToken ct = default);
    }
}
