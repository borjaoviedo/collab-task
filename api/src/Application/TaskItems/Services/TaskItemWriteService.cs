using Application.Abstractions.Persistence;
using Application.Common.Exceptions;
using Application.TaskActivities.Abstractions;
using Application.TaskActivities.Payloads;
using Application.TaskItems.Abstractions;
using Application.TaskItems.DTOs;
using Application.TaskItems.Mapping;
using Application.TaskItems.Realtime;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using MediatR;

namespace Application.TaskItems.Services
{
    /// <summary>
    /// Application write-side service for <see cref="TaskItem"/> aggregates.
    /// Handles creation, editing, movement, and deletion of task items within a project board,
    /// enforcing lane/column constraints, title uniqueness, and optimistic concurrency rules.
    /// Each successful write operation persists changes through <see cref="IUnitOfWork"/>,
    /// records a corresponding <see cref="TaskActivity"/> entry,
    /// and publishes MediatR notifications so that other components (such as real-time hubs)
    /// can react to task lifecycle events.
    /// </summary>
    /// <param name="taskItemRepository">
    /// Repository used to query, track, and persist <see cref="TaskItem"/> entities,
    /// including existence checks and retrieval for updates.
    /// </param>
    /// <param name="unitOfWork">
    /// Coordinates transactional persistence, ensuring that domain mutations are atomically applied
    /// and validated against concurrency tokens through <see cref="DomainMutation"/>.
    /// </param>
    /// <param name="taskActivityWriteService">
    /// Service responsible for emitting <see cref="TaskActivity"/> records that describe
    /// task-related events such as creation, edits, moves, and deletions.
    /// </param>
    /// <param name="mediator">
    /// MediatR abstraction used to publish domain notifications following successful write operations,
    /// enabling decoupled reactions across the application.
    /// </param>
    public sealed class TaskItemWriteService(
        ITaskItemRepository taskItemRepository,
        IUnitOfWork unitOfWork,
        ITaskActivityWriteService taskActivityWriteService,
        IMediator mediator) : ITaskItemWriteService
    {
        private readonly ITaskItemRepository _taskItemRepository = taskItemRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ITaskActivityWriteService _taskActivityWriteService = taskActivityWriteService;
        private readonly IMediator _mediator = mediator;

        /// <inheritdoc/>
        public async Task<TaskItemReadDto> CreateAsync(
            Guid projectId,
            Guid laneId,
            Guid columnId,
            Guid userId,
            TaskItemCreateDto dto,
            CancellationToken ct = default)
        {
            if (await _taskItemRepository.ExistsWithTitleAsync(columnId, dto.Title, ct: ct))
                throw new ConflictException("A task item with the specified title already exists.");

            var task = TaskItem.Create(
                columnId,
                laneId,
                projectId,
                TaskTitle.Create(dto.Title),
                TaskDescription.Create(dto.Description),
                dto.DueDate,
                dto.SortKey);
            await _taskItemRepository.AddAsync(task, ct);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Create, ct);
            if (mutation != DomainMutation.Created)
                throw new ConflictException("Task item could not be created due to a conflicting state.");

            var payload = ActivityPayloadFactory.TaskCreated(dto.Title);
            await _taskActivityWriteService.CreateAsync(
                task.Id,
                userId,
                TaskActivityType.TaskCreated,
                payload,
                ct);

            var notification = new TaskItemCreated(
                projectId,
                new TaskItemCreatedPayload(
                    task.Id,
                    task.ColumnId,
                    task.LaneId,
                    task.Title.Value,
                    task.Description.Value,
                    task.SortKey));

            await _mediator.Publish(notification, ct);

            return task.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<TaskItemReadDto> EditAsync(
            Guid projectId,
            Guid taskId,
            Guid userId,
            TaskItemEditDto dto,
            CancellationToken ct = default)
        {
            var task = await _taskItemRepository.GetByIdForUpdateAsync(taskId, ct)
                ?? throw new NotFoundException("Task item not found.");

            var oldTaskTitle = task.Title.Value;
            var oldTaskDescription = task.Description.Value;
            var taskTitleVo = TaskTitle.Create(dto.NewTitle!);
            var taskDescriptionVo = TaskDescription.Create(dto.NewDescription!);

            task.Edit(taskTitleVo, taskDescriptionVo, dto.NewDueDate);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Update, ct);
            if (mutation != DomainMutation.Updated)
                throw new ConflictException("The task item could not be edited due to a conflicting state.");

            var payload = ActivityPayloadFactory.TaskEdited(
                oldTaskTitle,
                dto.NewTitle,
                oldTaskDescription,
                dto.NewDescription);
            await _taskActivityWriteService.CreateAsync(
                taskId,
                userId,
                TaskActivityType.TaskEdited,
                payload,
                ct);

            var notification = new TaskItemUpdated(
                projectId,
                new TaskItemUpdatedPayload(
                    taskId,
                    dto.NewTitle,
                    dto.NewDescription,
                    dto.NewDueDate));
            await _mediator.Publish(notification, ct);

            return task.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<TaskItemReadDto> MoveAsync(
            Guid projectId,
            Guid taskId,
            Guid userId,
            TaskItemMoveDto dto,
            CancellationToken ct = default)
        {
            var task = await _taskItemRepository.GetByIdForUpdateAsync(taskId, ct)
                ?? throw new NotFoundException("Task item not found.");

            var oldColumn = task.ColumnId;
            var oldLane = task.LaneId;

            task.Move(dto.NewLaneId, dto.NewColumnId, dto.NewSortKey);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Update, ct);
            if (mutation != DomainMutation.Updated)
                throw new ConflictException("The task item could not be moved due to a conflicting state.");

            var payload = ActivityPayloadFactory.TaskMoved(
                oldLane,
                oldColumn,
                dto.NewLaneId,
                dto.NewColumnId);
            await _taskActivityWriteService.CreateAsync(
                taskId,
                userId,
                TaskActivityType.TaskMoved,
                payload,
                ct);

            var notification = new TaskItemMoved(
                projectId,
                new TaskItemMovedPayload(
                    taskId,
                    oldLane,
                    oldColumn,
                    dto.NewLaneId,
                    dto.NewColumnId,
                    dto.NewSortKey));
            await _mediator.Publish(notification, ct);

            return task.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task DeleteByIdAsync(
            Guid projectId,
            Guid taskId,
            CancellationToken ct = default)
        {
            var task = await _taskItemRepository.GetByIdForUpdateAsync(taskId, ct)
                ?? throw new NotFoundException("Task item not found.");

            await _taskItemRepository.RemoveAsync(task, ct);
            var mutation = await _unitOfWork.SaveAsync(MutationKind.Delete, ct);

            if (mutation != DomainMutation.Deleted && mutation != DomainMutation.NoOp)
                throw new ConflictException("The task item could not be deleted due to a conflicting state.");

            var notification = new TaskItemDeleted(projectId, new TaskItemDeletedPayload(taskId));
            await _mediator.Publish(notification, ct);
        }
    }
}
