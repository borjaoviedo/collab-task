using Application.Common.Abstractions.Extensions;
using Application.Common.Abstractions.Persistence;
using Application.TaskActivities.Abstractions;
using Application.TaskActivities.Payloads;
using Application.TaskAssignments.Abstractions;
using Application.TaskAssignments.Changes;
using Application.TaskAssignments.Realtime;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.TaskAssignments.Services
{
    /// <summary>
    /// Write-side application service for task assignments.
    /// </summary>
    public sealed class TaskAssignmentWriteService(
        ITaskAssignmentRepository repo,
        IUnitOfWork uow,
        ITaskActivityWriteService activityWriter,
        IMediator mediator) : ITaskAssignmentWriteService
    {
        /// <summary>
        /// Creates a new assignment for a target user and emits a creation activity and notification.
        /// </summary>
        public async Task<(DomainMutation, TaskAssignment?)> CreateAsync(
            Guid projectId,
            Guid taskId,
            Guid targetUserId,
            TaskRole role,
            Guid executedBy,
            CancellationToken ct = default)
        {
            var existing = await repo.GetTrackedByTaskAndUserIdAsync(taskId, targetUserId, ct);
            if (existing is not null)
            {
                if (existing.Role == role)
                    return (DomainMutation.NoOp, existing);

                return (DomainMutation.Conflict, existing);
            }

            var assignment = TaskAssignment.Create(taskId, targetUserId, role);
            await repo.AddAsync(assignment, ct);

            var createPayload = ActivityPayloadFactory.AssignmentCreated(targetUserId, role);
            await activityWriter.CreateAsync(
                taskId,
                executedBy,
                TaskActivityType.AssignmentCreated,
                createPayload,
                ct);

            var saveCreateResult = await uow.SaveAsync(MutationKind.Create, ct);

            if (saveCreateResult == DomainMutation.Created)
            {
                var createdNotification = new TaskAssignmentCreated(
                    projectId,
                    new TaskAssignmentCreatedPayload(taskId, targetUserId, role));
                await mediator.Publish(createdNotification, ct);
            }

            return (saveCreateResult, assignment);
        }

        /// <summary>
        /// Changes the role of an existing assignment, records an activity, and publishes a notification.
        /// </summary>
        public async Task<DomainMutation> ChangeRoleAsync(
            Guid projectId,
            Guid taskId,
            Guid targetUserId,
            TaskRole newRole,
            Guid executedBy,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var (changeRoleStatus, change) = await repo.ChangeRoleAsync(taskId, targetUserId, newRole, rowVersion, ct);
            if (changeRoleStatus != PrecheckStatus.Ready || change is null)
                return changeRoleStatus.ToErrorDomainMutation();

            var roleChange = (AssignmentRoleChangedChange)change;
            var payload = ActivityPayloadFactory.AssignmentRoleChanged(
                targetUserId,
                roleChange.OldRole,
                roleChange.NewRole);
            await activityWriter.CreateAsync(
                taskId,
                executedBy,
                TaskActivityType.AssignmentRoleChanged,
                payload,
                ct);

            var saveUpdateResult = await uow.SaveAsync(MutationKind.Update, ct);

            if (saveUpdateResult == DomainMutation.Updated)
            {
                var notification = new TaskAssignmentUpdated(
                    projectId,
                    new TaskAssignmentUpdatedPayload(taskId, targetUserId, newRole));
                await mediator.Publish(notification, ct);
            }

            return saveUpdateResult;
        }

        /// <summary>
        /// Deletes an assignment, records a removal activity, and publishes a notification.
        /// </summary>
        public async Task<DomainMutation> DeleteAsync(
            Guid projectId,
            Guid taskId,
            Guid targetUserId,
            Guid executedBy,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var deleteStatus = await repo.DeleteAsync(taskId, targetUserId, rowVersion, ct);
            if (deleteStatus != PrecheckStatus.Ready) return deleteStatus.ToErrorDomainMutation();

            var payload = ActivityPayloadFactory.AssignmentRemoved(targetUserId);
            await activityWriter.CreateAsync(
                taskId,
                executedBy,
                TaskActivityType.AssignmentRemoved,
                payload,
                ct);

            var saveDeleteResult = await uow.SaveAsync(MutationKind.Delete, ct);

            if (saveDeleteResult == DomainMutation.Deleted)
            {
                var notification = new TaskAssignmentRemoved(
                    projectId,
                    new TaskAssignmentRemovedPayload(taskId, targetUserId));
                await mediator.Publish(notification, ct);
            }

            return saveDeleteResult;
        }
    }

}
