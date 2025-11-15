using Application.Abstractions.Auth;
using Application.Common.Exceptions;
using Application.TaskActivities.Abstractions;
using Application.TaskActivities.DTOs;
using Application.TaskActivities.Mapping;
using Domain.Enums;

namespace Application.TaskActivities.Services
{
    /// <summary>
    /// Application read-side service for <see cref="Domain.Entities.TaskActivity"/> aggregates.
    /// Provides query operations for retrieving individual activity records, listing activities
    /// for a specific task, retrieving actions performed by a given user, and filtering activities
    /// by type. All retrieved entities are mapped to <see cref="TaskActivityReadDto"/> to provide
    /// a stable, client-friendly read model. Missing activities are surfaced as
    /// <see cref="NotFoundException"/>.
    /// </summary>
    /// <param name="taskActivityRepository">
    /// Repository used for querying <see cref="Domain.Entities.TaskActivity"/> entities,
    /// including lookups by identifier, task, user, and activity type.
    /// </param>
    /// <param name="currentUserService">
    /// Provides information about the currently authenticated user, such as <c>UserId</c>.
    /// </param>
    public sealed class TaskActivityReadService(
        ITaskActivityRepository taskActivityRepository,
        ICurrentUserService currentUserService) : ITaskActivityReadService
    {
        private readonly ITaskActivityRepository _taskActivityRepository = taskActivityRepository;
        private readonly ICurrentUserService _currentUserService = currentUserService;

        /// <inheritdoc/>
        public async Task<TaskActivityReadDto> GetByIdAsync(Guid activityId, CancellationToken ct = default)
        {
            var activity = await _taskActivityRepository.GetByIdAsync(activityId, ct)
                // 404 if the activity does not exist
                ?? throw new NotFoundException("Task activity not found.");

            return activity.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskActivityReadDto>> ListByTaskIdAsync(Guid taskId, CancellationToken ct = default)
        {
            var activities = await _taskActivityRepository.ListByTaskIdAsync(taskId, ct);

            return activities
                .Select(ta => ta.ToReadDto())
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskActivityReadDto>> ListByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            var activities = await _taskActivityRepository.ListByUserIdAsync(userId, ct);

            return activities
                .Select(ta => ta.ToReadDto())
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskActivityReadDto>> ListSelfAsync(CancellationToken ct = default)
        {
            var currentUserId = (Guid)_currentUserService.UserId!;
            var activities = await _taskActivityRepository.ListByUserIdAsync(currentUserId, ct);

            return activities
                .Select(ta => ta.ToReadDto())
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskActivityReadDto>> ListByActivityTypeAsync(
            Guid taskId,
            TaskActivityType type,
            CancellationToken ct = default)
        {
            var activities = await _taskActivityRepository.ListByTaskTypeAsync(taskId, type, ct);

            return activities
                .Select(ta => ta.ToReadDto())
                .ToList();
        }
    }
}
