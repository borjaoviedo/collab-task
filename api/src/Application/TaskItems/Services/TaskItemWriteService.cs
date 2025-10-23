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
    public sealed class TaskItemWriteService(ITaskItemRepository repo, ITaskActivityWriteService activityWriter, IMediator mediator) : ITaskItemWriteService
    {
        public async Task<(DomainMutation, TaskItem?)> CreateAsync(
            Guid projectId,
            Guid laneId,
            Guid columnId,
            Guid userId,
            TaskTitle title,
            TaskDescription description,
            DateTimeOffset? dueDate = null,
            decimal? sortKey = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(title)) return (DomainMutation.NoOp, null);
            if (await repo.ExistsWithTitleAsync(columnId, title, excludeTaskId: null, ct)) return (DomainMutation.Conflict, null);

            var key = sortKey ?? await repo.GetNextSortKeyAsync(columnId, ct);
            var task = TaskItem.Create(
                columnId,
                laneId,
                projectId,
                title,
                description,
                dueDate,
                key);
            await repo.AddAsync(task, ct);

            var payload = ActivityPayloadFactory.TaskCreated(title);
            await activityWriter.CreateAsync(task.Id, userId, TaskActivityType.TaskCreated, payload, ct);
            await repo.SaveCreateChangesAsync(ct);

            var notification = new TaskItemCreated(
                projectId,
                new TaskItemCreatedPayload(task.Id, task.ColumnId, task.LaneId, task.Title.Value, task.Description.Value, task.SortKey));
            await mediator.Publish(notification, ct);

            return (DomainMutation.Created, task);
        }

        public async Task<DomainMutation> EditAsync(
            Guid projectId,
            Guid taskId,
            Guid userId,
            TaskTitle? newTitle,
            TaskDescription? newDescription,
            DateTimeOffset? newDueDate,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var (editResult, change) = await repo.EditAsync(taskId, newTitle, newDescription, newDueDate, rowVersion, ct);
            if (editResult != DomainMutation.Updated || change is null) return editResult;

            var taskItemChange = (TaskItemEditedChange)change;
            var payload = ActivityPayloadFactory.TaskEdited(
                taskItemChange.OldTitle,
                taskItemChange.NewTitle,
                taskItemChange.OldDescription,
                taskItemChange.NewDescription);
            await activityWriter.CreateAsync(taskId, userId, TaskActivityType.TaskEdited, payload, ct);

            var saveUpdateResult = await repo.SaveUpdateChangesAsync(ct);

            if (saveUpdateResult == DomainMutation.Updated)
            {
                var notification = new TaskItemUpdated(
                    projectId,
                    new TaskItemUpdatedPayload(taskId, taskItemChange.NewTitle, taskItemChange.NewDescription, newDueDate));
                await mediator.Publish(notification, ct);
            }

            return saveUpdateResult;
        }

        public async Task<DomainMutation> MoveAsync(
            Guid projectId,
            Guid taskId,
            Guid targetColumnId,
            Guid targetLaneId,
            Guid userId,
            decimal targetSortKey,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var (moveResult, change) = await repo.MoveAsync(taskId, targetColumnId, targetLaneId, targetSortKey, rowVersion, ct);
            if (moveResult != DomainMutation.Updated || change is null) return moveResult;

            var taskItemChange = (TaskItemMovedChange)change;
            var payload = ActivityPayloadFactory.TaskMoved(
                taskItemChange.FromLaneId,
                taskItemChange.FromColumnId,
                taskItemChange.ToLaneId,
                taskItemChange.ToColumnId);
            await activityWriter.CreateAsync(taskId, userId, TaskActivityType.TaskMoved, payload, ct);

            var saveUpdateResult = await repo.SaveUpdateChangesAsync(ct);

            if (saveUpdateResult == DomainMutation.Updated)
            {
                var notification = new TaskItemMoved(
                    projectId,
                    new TaskItemMovedPayload(taskId, taskItemChange.FromLaneId, taskItemChange.FromColumnId, taskItemChange.ToLaneId, taskItemChange.ToColumnId, targetSortKey));
                await mediator.Publish(notification, ct);
            }

            return saveUpdateResult;
        }

        public async Task<DomainMutation> DeleteAsync(Guid projectId, Guid taskId, byte[] rowVersion, CancellationToken ct = default)
        {
            var deleteResult = await repo.DeleteAsync(taskId, rowVersion, ct);

            if (deleteResult == DomainMutation.Deleted)
            {
                var notification = new TaskItemDeleted(projectId, new TaskItemDeletedPayload(taskId));
                await mediator.Publish(notification, ct);
            }

            return deleteResult;
        }
    }
}
