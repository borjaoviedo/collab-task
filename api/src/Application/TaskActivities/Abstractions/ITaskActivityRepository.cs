using Domain.Entities;
using Domain.Enums;

namespace Application.TaskActivities.Abstractions
{
    /// <summary>
    /// Defines persistence operations for task activity entities.
    /// </summary>
    public interface ITaskActivityRepository
    {
        /// <summary>Lists activities related to a specific task.</summary>
        Task<IReadOnlyList<TaskActivity>> ListByTaskAsync(Guid taskId, CancellationToken ct = default);

        /// <summary>Lists activities performed by a specific user.</summary>
        Task<IReadOnlyList<TaskActivity>> ListByUserAsync(Guid userId, CancellationToken ct = default);

        /// <summary>Lists activities of a specific type for a task.</summary>
        Task<IReadOnlyList<TaskActivity>> ListByTypeAsync(Guid taskId, TaskActivityType type, CancellationToken ct = default);

        /// <summary>Gets a task activity by its identifier.</summary>
        Task<TaskActivity?> GetByIdAsync(Guid activityId, CancellationToken ct = default);

        /// <summary>Adds a new task activity record.</summary>
        Task AddAsync(TaskActivity activity, CancellationToken ct = default);
    }
}
