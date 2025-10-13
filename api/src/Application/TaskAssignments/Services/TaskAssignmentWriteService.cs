using Application.TaskAssignments.Abstractions;
using Domain.Entities;
using Domain.Enums;

namespace Application.TaskAssignments.Services
{
    public sealed class TaskAssignmentWriteService(ITaskAssignmentRepository repo) : ITaskAssignmentWriteService
    {
        public async Task<(DomainMutation, TaskAssignment?)> CreateAsync(Guid taskId, Guid userId, TaskRole role, CancellationToken ct = default)
        {
            // Delegate to Assign to enforce single-owner and dedupe semantics
            var existing = await repo.GetAsync(taskId, userId, ct);
            if (existing is not null)
            {
                var m = await repo.AssignAsync(taskId, userId, role, ct); // Created/Updated/NoOp/Conflict
                return (m, existing);
            }

            var m2 = await repo.AssignAsync(taskId, userId, role, ct);
            if (m2 == DomainMutation.Created)
            {
                var created = await repo.GetAsync(taskId, userId, ct);
                return (m2, created);
            }
            return (m2, null);
        }

        public async Task<DomainMutation> AssignAsync(Guid taskId, Guid userId, TaskRole role, CancellationToken ct = default)
            => await repo.AssignAsync(taskId, userId, role, ct);

        public async Task<DomainMutation> ChangeRoleAsync(Guid taskId, Guid userId, TaskRole newRole, byte[] rowVersion, CancellationToken ct = default)
            => await repo.ChangeRoleAsync(taskId, userId, newRole, rowVersion, ct);

        public async Task<DomainMutation> RemoveAsync(Guid taskId, Guid userId, byte[] rowVersion, CancellationToken ct = default)
            => await repo.RemoveAsync(taskId, userId, rowVersion, ct);
    }
}
