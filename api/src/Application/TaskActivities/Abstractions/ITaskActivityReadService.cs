using Domain.Entities;
using Domain.Enums;

namespace Application.TaskActivities.Abstractions
{
    /// <summary>
    /// Provides read-only access to task activity records.
    /// </summary>
    public interface ITaskActivityReadService
    {
        /// <summary>Retrieves a task activity by its unique identifier.</summary>
        Task<TaskActivity?> GetAsync(Guid activityId, CancellationToken ct = default);

        /// <summary>Lists all activities related to a given task.</summary>
        Task<IReadOnlyList<TaskActivity>> ListByTaskAsync(Guid taskId, CancellationToken ct = default);

        /// <summary>Lists all activities performed by a given user.</summary>
        Task<IReadOnlyList<TaskActivity>> ListByUserAsync(Guid userId, CancellationToken ct = default);

        /// <summary>Lists all activities of a specific type for a given task.</summary>
        Task<IReadOnlyList<TaskActivity>> ListByTypeAsync(Guid taskId, TaskActivityType type, CancellationToken ct = default);
    }
}
