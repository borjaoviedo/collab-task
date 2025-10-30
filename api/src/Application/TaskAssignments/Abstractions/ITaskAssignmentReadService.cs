using Domain.Entities;

namespace Application.TaskAssignments.Abstractions
{
    /// <summary>
    /// Provides read-only access to task assignments and user participation within tasks.
    /// </summary>
    public interface ITaskAssignmentReadService
    {
        /// <summary>Gets a task assignment by task and user identifiers.</summary>
        Task<TaskAssignment?> GetAsync(Guid taskId, Guid userId, CancellationToken ct = default);

        /// <summary>Lists all assignments for a given task.</summary>
        Task<IReadOnlyList<TaskAssignment>> ListByTaskAsync(Guid taskId, CancellationToken ct = default);

        /// <summary>Lists all assignments for a given user.</summary>
        Task<IReadOnlyList<TaskAssignment>> ListByUserAsync(Guid userId, CancellationToken ct = default);
    }
}
