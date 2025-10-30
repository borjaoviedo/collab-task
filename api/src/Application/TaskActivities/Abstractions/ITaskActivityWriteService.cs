using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskActivities.Abstractions
{
    /// <summary>
    /// Handles creation of task activity records at the application level.
    /// </summary>
    public interface ITaskActivityWriteService
    {
        /// <summary>Creates a new task activity associated with a task and user.</summary>
        Task<(DomainMutation, TaskActivity?)> CreateAsync(
            Guid taskId,
            Guid userId,
            TaskActivityType type,
            ActivityPayload payload,
            CancellationToken ct = default);
    }
}
