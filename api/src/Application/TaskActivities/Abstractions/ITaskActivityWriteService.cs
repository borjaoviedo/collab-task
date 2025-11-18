using Application.TaskActivities.DTOs;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskActivities.Abstractions
{
    /// <summary>
    /// Provides write operations for creating <see cref="Domain.Entities.TaskActivity"/> records.
    /// Task activities represent audit events associated with task actions,
    /// such as updates, assignments, moves, and note interactions.
    /// Each created activity is persisted through the underlying repository
    /// and returned as a <see cref="TaskActivityReadDto"/> for client consumption.
    /// </summary>
    public interface ITaskActivityWriteService
    {
        /// <summary>
        /// Creates a new activity entry associated with the specified task and user.
        /// The activity type determines the semantic meaning of the audit event,
        /// while the payload carries contextual information relevant to the action.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task for which the activity is created.</param>
        /// <param name="userId">The unique identifier of the user performing the activity.</param>
        /// <param name="type">The type of activity being recorded.</param>
        /// <param name="payload">Additional contextual data associated with the activity.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="TaskActivityReadDto"/> representing the newly created activity entry.
        /// </returns>
        Task<TaskActivityReadDto> CreateAsync(
            Guid taskId,
            Guid userId,
            TaskActivityType type,
            ActivityPayload payload,
            CancellationToken ct = default);
    }
}
