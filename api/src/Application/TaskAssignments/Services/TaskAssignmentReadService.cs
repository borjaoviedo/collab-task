using Application.Abstractions.Auth;
using Application.Common.Exceptions;
using Application.TaskAssignments.Abstractions;
using Application.TaskAssignments.DTOs;
using Application.TaskAssignments.Mapping;

namespace Application.TaskAssignments.Services
{
    /// <summary>
    /// Application read-side service for <see cref="Domain.Entities.TaskAssignment"/> aggregates.
    /// Provides query operations for retrieving assignment details for a specific task–user pair,
    /// as well as listing all assignments for a given task or a given user. All results are mapped
    /// to <see cref="TaskAssignmentReadDto"/> to provide a stable, client-facing read model.
    /// Missing records are surfaced as <see cref="NotFoundException"/> to ensure
    /// consistent error behavior across the application.
    /// </summary>
    /// <param name="taskAssignmentRepository">
    /// Repository used for querying <see cref="Domain.Entities.TaskAssignment"/> entities,
    /// including lookups by task/user, listings by task, and listings by user.
    /// </param>
    /// <param name="currentUserService">
    /// Provides information about the currently authenticated user, such as <c>UserId</c>.
    /// </param>
    public sealed class TaskAssignmentReadService(
        ITaskAssignmentRepository taskAssignmentRepository,
        ICurrentUserService currentUserService) : ITaskAssignmentReadService
    {
        private readonly ITaskAssignmentRepository _taskAssignmentRepository = taskAssignmentRepository;
        private readonly ICurrentUserService _currentUserService = currentUserService;

        /// <inheritdoc/>
        public async Task<TaskAssignmentReadDto> GetByTaskAndUserIdAsync(
            Guid taskId,
            Guid userId,
            CancellationToken ct = default)
        {
            var assignment = await _taskAssignmentRepository.GetByTaskAndUserIdAsync(taskId, userId, ct)
                // 404 if the assignment does not exist
                ?? throw new NotFoundException("Task assignment not found.");

            return assignment.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskAssignmentReadDto>> ListByTaskIdAsync(
            Guid taskId,
            CancellationToken ct = default)
        {
            var assignments = await _taskAssignmentRepository.ListByTaskIdAsync(taskId, ct);

            return assignments
                .Select(ta => ta.ToReadDto())
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskAssignmentReadDto>> ListByUserIdAsync(
            Guid userId,
            CancellationToken ct = default)
        {
            var assignments = await _taskAssignmentRepository.ListByUserIdAsync(userId, ct);

            return assignments
                .Select(ta => ta.ToReadDto())
                .ToList();
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<TaskAssignmentReadDto>> ListSelfAsync(
            CancellationToken ct = default)
        {
            var currentUserId = (Guid)_currentUserService.UserId!;
            var assignments = await _taskAssignmentRepository.ListByUserIdAsync(currentUserId, ct);

            return assignments
                .Select(ta => ta.ToReadDto())
                .ToList();
        }
    }
}
