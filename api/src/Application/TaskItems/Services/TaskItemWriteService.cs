using Application.Common.Changes;
using Application.TaskActivities;
using Application.TaskActivities.Abstractions;
using Application.TaskItems.Abstractions;
using Application.TaskItems.Realtime;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using MediatR;

namespace Application.TaskItems.Services
{
    public sealed class TaskItemWriteService(
        ITaskItemRepository repo, ITaskActivityWriteService activityWriter, IMediator mediator) : ITaskItemWriteService
    {
        public async Task<(DomainMutation, TaskItem?)> CreateAsync(Guid projectId, Guid laneId, Guid columnId, Guid userId, string title,
            string description, DateTimeOffset? dueDate = null, decimal? sortKey = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(title)) return (DomainMutation.NoOp, null);

            if (await repo.ExistsWithTitleAsync(columnId, title, ct: ct))
                return (DomainMutation.Conflict, null);

            var key = sortKey ?? await repo.GetNextSortKeyAsync(columnId, ct);
            var task = TaskItem.Create(columnId, laneId, projectId, TaskTitle.Create(title), TaskDescription.Create(description), dueDate, key);
            await repo.AddAsync(task, ct);

            var payload = ActivityPayloadFactory.TaskCreated(title);
            await activityWriter.CreateAsync(task.Id, userId, TaskActivityType.TaskCreated, payload, ct);

            await repo.SaveCreateChangesAsync(ct);
            await mediator.Publish(
                new TaskItemCreated(task.ProjectId,
                    new TaskItemCreatedPayload(task.Id, task.ColumnId, task.LaneId, task.Title.Value, task.Description.Value, task.SortKey)),
                ct);

            return (DomainMutation.Created, task);
        }

        public async Task<DomainMutation> EditAsync(Guid taskId, Guid userId, string? newTitle, string? newDescription, DateTimeOffset? newDueDate,
            byte[] rowVersion, CancellationToken ct = default)
        {
            var (mutation, change) = await repo.EditAsync(taskId, newTitle, newDescription, newDueDate, rowVersion, ct);
            if (mutation != DomainMutation.Updated) return mutation;

            var c = (TaskItemEditedChange)change!;
            var payload = ActivityPayloadFactory.TaskEdited(c.OldTitle, c.NewTitle, c.OldDescription, c.NewDescription);
            
            await activityWriter.CreateAsync(taskId, userId, TaskActivityType.TaskEdited, payload, ct);

            var saved = await repo.SaveUpdateChangesAsync(ct);
            if (saved == DomainMutation.Updated)
            {
                var task = await repo.GetByIdAsync(taskId, ct);
                await mediator.Publish(
                    new TaskItemUpdated(task!.ProjectId,
                        new TaskItemUpdatedPayload(taskId, c.NewTitle, c.NewDescription, newDueDate)),
                    ct);
            }

            return saved;
        }

        public async Task<DomainMutation> MoveAsync(Guid taskId, Guid targetColumnId, Guid targetLaneId, Guid userId,
            decimal targetSortKey, byte[] rowVersion, CancellationToken ct = default)
        {
            var (mutation, change) = await repo.MoveAsync(taskId, targetColumnId, targetLaneId, targetSortKey, rowVersion, ct);
            if (mutation != DomainMutation.Updated) return mutation;

            var c = (TaskItemMovedChange)change!;
            var payload = ActivityPayloadFactory.TaskMoved(c.FromLaneId, c.FromColumnId, c.ToLaneId, c.ToColumnId);

            await activityWriter.CreateAsync(taskId, userId, TaskActivityType.TaskMoved, payload, ct);
            var result = await repo.SaveUpdateChangesAsync(ct);

            if (result == DomainMutation.Updated)
            {
                var task = await repo.GetByIdAsync(taskId, ct);
                await mediator.Publish(
                    new TaskItemMoved(task!.ProjectId,
                        new TaskItemMovedPayload(taskId, c.FromLaneId, c.FromColumnId, c.ToLaneId, c.ToColumnId, targetSortKey)),
                    ct);
            }
            return result;
        }

        public Task<DomainMutation> DeleteAsync(Guid taskId, byte[] rowVersion, CancellationToken ct = default)
            => repo.DeleteAsync(taskId, rowVersion, ct);
    }
}
