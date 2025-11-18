using Application.Abstractions.Time;
using Application.TaskActivities.Abstractions;
using Application.TaskActivities.DTOs;
using Application.TaskActivities.Mapping;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskActivities.Services
{
    /// <summary>
    /// Application write-side service for <see cref="TaskActivity"/> aggregates.
    /// Responsible for creating immutable activity records that capture user-initiated events
    /// on tasks—such as edits, moves, assignments, and notes. Activities serve as the audit log
    /// for the task board and are timestamped using <see cref="IDateTimeProvider"/> to ensure
    /// deterministic and testable time generation.
    /// </summary>
    /// <param name="taskActivityRepository">
    /// Repository used to persist <see cref="TaskActivity"/> entities
    /// representing the audit trail of task-related operations.
    /// </param>
    /// <param name="dateTimeProvider">
    /// Abstraction for obtaining UTC timestamps, ensuring consistency across environments
    /// and enabling deterministic behavior in tests.
    /// </param>
    public sealed class TaskActivityWriteService(
        ITaskActivityRepository taskActivityRepository,
        IDateTimeProvider dateTimeProvider) : ITaskActivityWriteService
    {
        private readonly ITaskActivityRepository _taskActivityRepository = taskActivityRepository;
        private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

        /// <inheritdoc/>
        public async Task<TaskActivityReadDto> CreateAsync(
            Guid taskId,
            Guid userId,
            TaskActivityType type,
            ActivityPayload payload,
            CancellationToken ct = default)
        {
            var activity = TaskActivity.Create(
                taskId,
                userId,
                type,
                payload,
                _dateTimeProvider.UtcNow);

            await _taskActivityRepository.AddAsync(activity, ct);

            return activity.ToReadDto();
        }
    }
}
