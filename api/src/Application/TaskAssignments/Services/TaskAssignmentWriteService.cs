using Application.Abstractions.Persistence;
using Application.Common.Exceptions;
using Application.TaskActivities.Abstractions;
using Application.TaskActivities.Payloads;
using Application.TaskAssignments.Abstractions;
using Application.TaskAssignments.DTOs;
using Application.TaskAssignments.Mapping;
using Application.TaskAssignments.Realtime;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.TaskAssignments.Services
{
    /// <summary>
    /// Application write-side service for <see cref="TaskAssignment"/> aggregates.
    /// Handles creation, role changes, and deletion of task-to-user assignments,
    /// enforcing assignment invariants such as uniqueness, single-owner constraints,
    /// and optimistic concurrency validation. Each successful write operation is
    /// persisted atomically through <see cref="IUnitOfWork"/>, emits a corresponding
    /// <see cref="TaskActivity"/> audit record, and publishes MediatR
    /// notifications to allow other system components (such as real-time hubs) to react
    /// to assignment lifecycle events.
    /// </summary>
    /// <param name="taskAssignmentRepository">
    /// Repository responsible for querying, tracking, and persisting
    /// <see cref="TaskAssignment"/> entities, including uniqueness checks
    /// and tracked retrieval for updates.
    /// </param>
    /// <param name="unitOfWork">
    /// Coordinates transactional persistence and maps <see cref="DomainMutation"/> results
    /// to optimistic concurrency handling for assignment operations.
    /// </param>
    /// <param name="taskActivityWriteService">
    /// Service used to record <see cref="TaskActivity"/> entries describing
    /// assignment-related events such as creation, role changes, or removal.
    /// </param>
    /// <param name="mediator">
    /// MediatR abstraction for publishing domain notifications after successful write operations,
    /// enabling decoupled reactions across the application.
    /// </param>
    public sealed class TaskAssignmentWriteService(
        ITaskAssignmentRepository taskAssignmentRepository,
        IUnitOfWork unitOfWork,
        ITaskActivityWriteService taskActivityWriteService,
        IMediator mediator) : ITaskAssignmentWriteService
    {
        private readonly ITaskAssignmentRepository _taskAssignmentRepository = taskAssignmentRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ITaskActivityWriteService _taskActivityWriteService = taskActivityWriteService;
        private readonly IMediator _mediator = mediator;

        /// <inheritdoc/>
        public async Task<TaskAssignmentReadDto> CreateAsync(
            Guid projectId,
            Guid taskId,
            Guid executedBy,
            TaskAssignmentCreateDto dto,
            CancellationToken ct = default)
        {
            if (await _taskAssignmentRepository.GetByTaskAndUserIdAsync(taskId, dto.UserId, ct) is not null)
                throw new ConflictException("A task assignment with the specified user already exists.");

            var assignment = TaskAssignment.Create(taskId, dto.UserId, dto.Role);
            await _taskAssignmentRepository.AddAsync(assignment, ct);

            var createPayload = ActivityPayloadFactory.AssignmentCreated(dto);
            await _taskActivityWriteService.CreateAsync(
                taskId,
                executedBy,
                TaskActivityType.AssignmentCreated,
                createPayload,
                ct);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Create, ct);
            if (mutation != DomainMutation.Created)
                throw new ConflictException("Task assignment could not be created due to a conflicting state.");

            var createdNotification = new TaskAssignmentCreated(
                projectId,
                new TaskAssignmentCreatedPayload(taskId, dto.UserId, dto.Role));
            await _mediator.Publish(createdNotification, ct);

            return assignment.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<TaskAssignmentReadDto> ChangeRoleAsync(
            Guid projectId,
            Guid taskId,
            Guid targetUserId,
            Guid executedBy,
            TaskAssignmentChangeRoleDto dto,
            CancellationToken ct = default)
        {
            var taskAssignment = await _taskAssignmentRepository.GetByTaskAndUserIdForUpdateAsync(taskId, targetUserId, ct)
                ?? throw new NotFoundException("Task assignment not found.");

            var oldRole = taskAssignment.Role;

            taskAssignment.ChangeRole(dto.NewRole);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Update, ct);
            if (mutation != DomainMutation.Updated)
                throw new ConflictException("The task assignment could not be updated due to a conflicting state.");

            var payload = ActivityPayloadFactory.AssignmentRoleChanged(
                targetUserId,
                oldRole,
                dto.NewRole);
            await _taskActivityWriteService.CreateAsync(
                taskId,
                executedBy,
                TaskActivityType.AssignmentRoleChanged,
                payload,
                ct);

            var notification = new TaskAssignmentUpdated(
                projectId,
                new TaskAssignmentUpdatedPayload(taskId, targetUserId, dto.NewRole));
            await _mediator.Publish(notification, ct);

            return taskAssignment.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(
            Guid projectId,
            Guid taskId,
            Guid targetUserId,
            Guid executedBy,
            CancellationToken ct = default)
        {
            var taskAssignment = await _taskAssignmentRepository.GetByTaskAndUserIdForUpdateAsync(taskId, targetUserId, ct)
                ?? throw new NotFoundException("Task assignment not found.");

            await _taskAssignmentRepository.RemoveAsync(taskAssignment, ct);
            var mutation = await _unitOfWork.SaveAsync(MutationKind.Delete, ct);

            if (mutation != DomainMutation.Deleted && mutation != DomainMutation.NoOp)
                throw new ConflictException("The task assignment could not be deleted due to a conflicting state.");

            if (mutation == DomainMutation.Deleted)
            {
                var payload = ActivityPayloadFactory.AssignmentRemoved(targetUserId);
                await _taskActivityWriteService.CreateAsync(
                    taskId,
                    executedBy,
                    TaskActivityType.AssignmentRemoved,
                    payload,
                    ct);

                var notification = new TaskAssignmentRemoved(
                    projectId,
                    new TaskAssignmentRemovedPayload(taskId, targetUserId));
                await _mediator.Publish(notification, ct);
            }
        }
    }
}
