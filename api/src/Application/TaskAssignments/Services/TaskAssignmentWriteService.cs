using Application.Common.Changes;
using Application.TaskActivities;
using Application.TaskActivities.Abstractions;
using Application.TaskAssignments.Abstractions;
using Application.TaskAssignments.Realtime;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.TaskAssignments.Services
{
    public sealed class TaskAssignmentWriteService(
        ITaskAssignmentRepository repo,
        ITaskActivityWriteService activityWriter,
        IMediator mediator) : ITaskAssignmentWriteService
    {
        public async Task<(DomainMutation, TaskAssignment?)> CreateAsync(
            Guid projectId,
            Guid taskId,
            Guid targetUserId,
            TaskRole role,
            Guid executedBy,
            CancellationToken ct = default)
        {
            var (assignResult, change) = await repo.AssignAsync(taskId, targetUserId, role, ct);
            if (change is null) return (assignResult, null);

            if (assignResult == DomainMutation.Created)
            {
                var payload = ActivityPayloadFactory.AssignmentCreated(targetUserId, role);

                await activityWriter.CreateAsync(taskId, executedBy, TaskActivityType.AssignmentCreated, payload, ct);
                await repo.SaveCreateChangesAsync(ct);

                var created = await repo.GetAsync(taskId, targetUserId, ct);
                var notification = new TaskAssignmentCreated(
                    projectId,
                    new TaskAssignmentCreatedPayload(taskId, targetUserId, role));
                await mediator.Publish(notification, ct);

                return (assignResult, created);
            }

            if (assignResult == DomainMutation.Updated)
            {
                var roleChange = (AssignmentRoleChangedChange)change;
                var payload = ActivityPayloadFactory.AssignmentRoleChanged(targetUserId, roleChange.OldRole, roleChange.NewRole);

                await activityWriter.CreateAsync(taskId, executedBy, TaskActivityType.AssignmentRoleChanged, payload, ct);
                await repo.SaveUpdateChangesAsync(ct);

                var updated = await repo.GetAsync(taskId, targetUserId, ct);
                var notification = new TaskAssignmentUpdated(
                    projectId,
                    new TaskAssignmentUpdatedPayload(taskId, targetUserId, roleChange.NewRole));
                await mediator.Publish(notification, ct);

                return (assignResult, updated);
            }

            return (assignResult, null);
        }

        public async Task<DomainMutation> AssignAsync(
            Guid projectId,
            Guid taskId,
            Guid targetUserId,
            TaskRole role,
            Guid executedBy,
            CancellationToken ct = default)
        {
            var (assignResult, change) = await repo.AssignAsync(taskId, targetUserId, role, ct);
            if (change is null) return assignResult;

            if (assignResult == DomainMutation.Created)
            {
                var payload = ActivityPayloadFactory.AssignmentCreated(targetUserId, role);

                await activityWriter.CreateAsync(taskId, executedBy, TaskActivityType.AssignmentCreated, payload, ct);
                await repo.SaveCreateChangesAsync(ct);

                var notification = new TaskAssignmentCreated(
                    projectId,
                    new TaskAssignmentCreatedPayload(taskId, targetUserId, role));
                await mediator.Publish(notification, ct);
            }

            else if (assignResult == DomainMutation.Updated)
            {
                var roleChange = (AssignmentRoleChangedChange)change;
                var payload = ActivityPayloadFactory.AssignmentRoleChanged(targetUserId, roleChange.OldRole, roleChange.NewRole);

                await activityWriter.CreateAsync(taskId, executedBy, TaskActivityType.AssignmentRoleChanged, payload, ct);
                await repo.SaveUpdateChangesAsync(ct);

                var notification = new TaskAssignmentUpdated(
                    projectId,
                    new TaskAssignmentUpdatedPayload(taskId, targetUserId, roleChange.NewRole));
                await mediator.Publish(notification, ct);
            }

            return assignResult;
        }

        public async Task<DomainMutation> ChangeRoleAsync(
            Guid projectId,
            Guid taskId,
            Guid targetUserId,
            TaskRole newRole,
            Guid executedBy,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var (changeRoleResult, change) = await repo.ChangeRoleAsync(taskId, targetUserId, newRole, rowVersion, ct);
            if (changeRoleResult != DomainMutation.Updated || change is null) return changeRoleResult;

            var roleChange = (AssignmentRoleChangedChange)change;
            var payload = ActivityPayloadFactory.AssignmentRoleChanged(targetUserId, roleChange.OldRole, roleChange.NewRole);
            await activityWriter.CreateAsync(taskId, executedBy, TaskActivityType.AssignmentRoleChanged, payload, ct);

            var saveUpdateResult = await repo.SaveUpdateChangesAsync(ct);

            if (saveUpdateResult == DomainMutation.Updated)
            {
                var notification = new TaskAssignmentUpdated(
                    projectId,
                    new TaskAssignmentUpdatedPayload(taskId, targetUserId, newRole));
                await mediator.Publish(notification, ct);
            }

            return saveUpdateResult;
        }

        public async Task<DomainMutation> RemoveAsync(
            Guid projectId,
            Guid taskId,
            Guid targetUserId,
            Guid executedBy,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var removeResult = await repo.RemoveAsync(taskId, targetUserId, rowVersion, ct);
            if (removeResult != DomainMutation.Deleted) return removeResult;

            var payload = ActivityPayloadFactory.AssignmentRemoved(targetUserId);
            await activityWriter.CreateAsync(taskId, executedBy, TaskActivityType.AssignmentRemoved, payload, ct);

            var saveRemoveResult = await repo.SaveRemoveChangesAsync(ct);

            if (saveRemoveResult == DomainMutation.Deleted)
            {
                var notification = new TaskAssignmentRemoved(projectId, new TaskAssignmentRemovedPayload(taskId, targetUserId));
                await mediator.Publish(notification, ct);
            }

            return saveRemoveResult;
        }
    }
}
