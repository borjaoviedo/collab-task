using Domain.Entities;

namespace Application.TaskAssignments.Abstractions
{
    /// <summary>
    /// Defines persistence operations for <see cref="TaskAssignment"/> entities,
    /// including lookup by task or user, owner-role verification, tracked and untracked
    /// retrieval workflows, and CRUD operations. Assignments represent the relationship
    /// between users and tasks, including ownership and co-ownership, and are central
    /// to permission enforcement and task collaboration logic.
    /// </summary>
    public interface ITaskAssignmentRepository
    {
        /// <summary>
        /// Retrieves all assignment records associated with a specific task.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task whose assignments will be retrieved.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// A read-only list of <see cref="TaskAssignment"/> entities associated with the given task.
        /// </returns>
        Task<IReadOnlyList<TaskAssignment>> ListByTaskIdAsync(
            Guid taskId,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves all assignment records associated with a specific user across all tasks.
        /// Useful for building a user-centric task overview or assignment dashboard.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose assignments will be retrieved.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// A read-only list of <see cref="TaskAssignment"/> entities linked to the specified user.
        /// </returns>
        Task<IReadOnlyList<TaskAssignment>> ListByUserIdAsync(
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves an assignment by task and user identifiers without enabling EF Core tracking.
        /// Use this for read-only scenarios or validations where mutation is not required.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The <see cref="TaskAssignment"/> entity, or <c>null</c> if no matching record is found.
        /// </returns>
        Task<TaskAssignment?> GetByTaskAndUserIdAsync(
            Guid taskId,
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves an assignment by task and user identifiers with EF Core tracking enabled.
        /// Use this before modifying the assignment so EF Core can detect changed values
        /// and generate minimal UPDATE statements during persistence.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The tracked <see cref="TaskAssignment"/> entity, or <c>null</c> if no matching record exists.
        /// </returns>
        Task<TaskAssignment?> GetByTaskAndUserIdForUpdateAsync(
            Guid taskId,
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Determines whether an assignment exists for the given task and user identifiers.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// <c>true</c> if the assignment exists; otherwise, <c>false</c>.
        /// </returns>
        Task<bool> ExistsAsync(
            Guid taskId,
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Determines whether the specified task already has an owner.
        /// Optionally excludes a given user when checking (e.g., during reassignment).
        /// Used to enforce the domain invariant that a task must have exactly one owner.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task.</param>
        /// <param name="excludeUserId">
        /// Optional user identifier to exclude from the ownership check.
        /// </param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// <c>true</c> if the task has an existing owner (excluding the provided user when applicable);
        /// otherwise <c>false</c>.
        /// </returns>
        Task<bool> AnyOwnerAsync(
            Guid taskId,
            Guid? excludeUserId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Adds a new task assignment record to the persistence context.
        /// </summary>
        /// <param name="assignment">The assignment entity to add.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task AddAsync(TaskAssignment assignment, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing <see cref="TaskAssignment"/> entity within the persistence context.
        /// EF Core change tracking will detect modifications and generate minimal UPDATE statements.
        /// </summary>
        /// <param name="assignment">The assignment entity with updated state.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task UpdateAsync(TaskAssignment assignment, CancellationToken ct = default);

        /// <summary>
        /// Removes a task assignment record from the persistence context.
        /// Actual deletion occurs when the surrounding <c>IUnitOfWork</c> commits changes.
        /// </summary>
        /// <param name="assignment">The assignment entity to remove.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task RemoveAsync(TaskAssignment assignment, CancellationToken ct = default);
    }
}
