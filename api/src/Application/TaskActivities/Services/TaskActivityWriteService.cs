using Application.Common.Abstractions.Time;
using Application.TaskActivities.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskActivities.Services
{
    /// <summary>
    /// Write-side application service for task activities.
    /// </summary>
    public sealed class TaskActivityWriteService(
        ITaskActivityRepository repo,
        IDateTimeProvider clock) : ITaskActivityWriteService
    {
        /// <summary>
        /// Creates a new activity entry for a task and user.
        /// </summary>
        /// <param name="taskId">Task identifier.</param>
        /// <param name="userId">Actor user identifier.</param>
        /// <param name="type">Activity type.</param>
        /// <param name="payload">Structured activity payload.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The mutation result and the created activity.</returns>
        public async Task<(DomainMutation, TaskActivity?)> CreateAsync(
            Guid taskId,
            Guid userId,
            TaskActivityType type,
            ActivityPayload payload,
            CancellationToken ct = default)
        {
            var activity = TaskActivity.Create(taskId, userId, type, payload, clock.UtcNow);

            await repo.AddAsync(activity, ct);
            return (DomainMutation.Created, activity);
        }
    }

}
