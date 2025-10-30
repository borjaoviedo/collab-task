using Application.TaskAssignments.Abstractions;
using Domain.Entities;

namespace Application.TaskAssignments.Services
{
    /// <summary>
    /// Read-only application service for task assignments.
    /// </summary>
    public sealed class TaskAssignmentReadService(ITaskAssignmentRepository repo) : ITaskAssignmentReadService
    {
        /// <summary>Retrieves an assignment by task and user identifiers.</summary>
        public async Task<TaskAssignment?> GetAsync(Guid taskId, Guid userId, CancellationToken ct = default)
            => await repo.GetByTaskAndUserIdAsync(taskId, userId, ct);

        /// <summary>Lists all assignments for a task.</summary>
        public async Task<IReadOnlyList<TaskAssignment>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => await repo.ListByTaskAsync(taskId, ct);

        /// <summary>Lists all assignments for a user.</summary>
        public async Task<IReadOnlyList<TaskAssignment>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => await repo.ListByUserAsync(userId, ct);
    }

}
