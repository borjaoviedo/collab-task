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
        ITaskAssignmentRepository repo, ITaskActivityWriteService activityWriter, IMediator mediator) : ITaskAssignmentWriteService
    {
        public async Task<(DomainMutation, TaskAssignment?)> CreateAsync(
            Guid projectId, Guid taskId, Guid affectedUserId, TaskRole role, Guid performedBy, CancellationToken ct = default)
        {
            var (m, change) = await repo.AssignAsync(taskId, affectedUserId, role, ct);
            switch (m)
            {
                case DomainMutation.Created:
                    await activityWriter.CreateAsync(taskId, performedBy, TaskActivityType.AssignmentCreated,
                        ActivityPayloadFactory.AssignmentCreated(affectedUserId, role), ct);
                    await repo.SaveCreateChangesAsync(ct);
                    var created = await repo.GetAsync(taskId, affectedUserId, ct);
                    await mediator.Publish(
                        new TaskAssignmentCreated(projectId,
                            new TaskAssignmentCreatedPayload(taskId, affectedUserId, role)), ct);
                    return (DomainMutation.Created, created);

                case DomainMutation.Updated:
                    var c = (AssignmentRoleChangedChange)change!;
                    await activityWriter.CreateAsync(taskId, performedBy, TaskActivityType.AssignmentRoleChanged,
                        ActivityPayloadFactory.AssignmentRoleChanged(affectedUserId, c.OldRole, c.NewRole), ct);
                    await repo.SaveUpdateChangesAsync(ct);
                    var updated = await repo.GetAsync(taskId, affectedUserId, ct);
                    await mediator.Publish(
                        new TaskAssignmentUpdated(projectId,
                            new TaskAssignmentUpdatedPayload(taskId, affectedUserId, c.NewRole)), ct);

                    return (DomainMutation.Updated, updated);

                default:
                    return (m, null);
            }
        }

        public async Task<DomainMutation> AssignAsync(
            Guid projectId, Guid taskId, Guid affectedUserId, TaskRole role, Guid performedBy, CancellationToken ct = default)
        {
            var (m, change) = await repo.AssignAsync(taskId, affectedUserId, role, ct);
            if (m == DomainMutation.Created)
            {
                await activityWriter.CreateAsync(taskId, performedBy, TaskActivityType.AssignmentCreated,
                    ActivityPayloadFactory.AssignmentCreated(affectedUserId, role), ct);
                await repo.SaveCreateChangesAsync(ct);
                await mediator.Publish(
                new TaskAssignmentCreated(projectId,
                    new TaskAssignmentCreatedPayload(taskId, affectedUserId, role)), ct);
            }
            else if (m == DomainMutation.Updated)
            {
                var c = (AssignmentRoleChangedChange)change!;
                await activityWriter.CreateAsync(taskId, performedBy, TaskActivityType.AssignmentRoleChanged,
                    ActivityPayloadFactory.AssignmentRoleChanged(affectedUserId, c.OldRole, c.NewRole), ct);
                await repo.SaveUpdateChangesAsync(ct);
                await mediator.Publish(
                new TaskAssignmentUpdated(projectId,
                    new TaskAssignmentUpdatedPayload(taskId, affectedUserId, c.NewRole)), ct);
            }
            return m;
        }

        public async Task<DomainMutation> ChangeRoleAsync(
            Guid projectId, Guid taskId, Guid affectedUserId, TaskRole newRole, Guid performedBy, byte[] rowVersion, CancellationToken ct = default)
        {
            var (m, change) = await repo.ChangeRoleAsync(taskId, affectedUserId, newRole, rowVersion, ct);
            if (m != DomainMutation.Updated) return m;

            var c = (AssignmentRoleChangedChange)change!;
            await activityWriter.CreateAsync(taskId, performedBy, TaskActivityType.AssignmentRoleChanged,
                ActivityPayloadFactory.AssignmentRoleChanged(affectedUserId, c.OldRole, c.NewRole), ct);

            var saved = await repo.SaveUpdateChangesAsync(ct);
            if (saved == DomainMutation.Updated)
            {
                await mediator.Publish(
                    new TaskAssignmentUpdated(projectId,
                        new TaskAssignmentUpdatedPayload(taskId, affectedUserId, newRole)), ct);
            }
            return saved;
        }

        public async Task<DomainMutation> RemoveAsync(
            Guid projectId, Guid taskId, Guid affectedUserId, Guid performedBy, byte[] rowVersion, CancellationToken ct = default)
        {
            var m = await repo.RemoveAsync(taskId, affectedUserId, rowVersion, ct);
            if (m != DomainMutation.Deleted) return m;

            await activityWriter.CreateAsync(taskId, performedBy, TaskActivityType.AssignmentRemoved,
                ActivityPayloadFactory.AssignmentRemoved(affectedUserId), ct);

            var saved = await repo.SaveRemoveChangesAsync(ct);
            if (saved == DomainMutation.Deleted)
            {
                await mediator.Publish(
                    new TaskAssignmentRemoved(projectId,
                        new TaskAssignmentRemovedPayload(taskId, affectedUserId)), ct);
            }
            return saved;
        }
    }
}
