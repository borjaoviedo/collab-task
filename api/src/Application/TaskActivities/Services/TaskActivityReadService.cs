using Application.TaskActivities.Abstractions;
using Domain.Entities;
using Domain.Enums;

namespace Application.TaskActivities.Services
{
    /// <summary>
    /// Read-only application service for task activities.
    /// </summary>
    public sealed class TaskActivityReadService(ITaskActivityRepository repo) : ITaskActivityReadService
    {
        /// <summary>Retrieves a task activity by its identifier.</summary>
        public async Task<TaskActivity?> GetAsync(Guid activityId, CancellationToken ct = default)
            => await repo.GetByIdAsync(activityId, ct);

        /// <summary>Lists activities associated with a specific task.</summary>
        public async Task<IReadOnlyList<TaskActivity>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => await repo.ListByTaskAsync(taskId, ct);

        /// <summary>Lists activities performed by a specific user.</summary>
        public async Task<IReadOnlyList<TaskActivity>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => await repo.ListByUserAsync(userId, ct);

        /// <summary>Lists activities of a given type for the specified task.</summary>
        public async Task<IReadOnlyList<TaskActivity>> ListByTypeAsync(
            Guid taskId,
            TaskActivityType type,
            CancellationToken ct = default)
            => await repo.ListByTypeAsync(taskId, type, ct);
    }
}
