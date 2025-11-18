using Domain.Entities;

namespace Application.TaskNotes.Abstractions
{
    /// <summary>
    /// Defines persistence operations for <see cref="TaskNote"/> entities,
    /// including retrieval by task or user, tracked and untracked lookup,
    /// and CRUD operations. Notes serve as user-authored comments or annotations
    /// attached to a task, forming part of the task’s activity history and
    /// collaboration workflow. This repository provides efficient access methods
    /// for listing, reading, creating, updating, and deleting notes within the
    /// persistence layer.
    /// </summary>
    public interface ITaskNoteRepository
    {
        /// <summary>
        /// Retrieves all notes associated with the specified task.
        /// Results are typically ordered chronologically by the caller.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task whose notes will be retrieved.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// A read-only list of <see cref="TaskNote"/> entities associated with the given task.
        /// </returns>
        Task<IReadOnlyList<TaskNote>> ListByTaskIdAsync(
            Guid taskId,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves all notes authored by the specified user across all tasks.
        /// Useful for personal activity feeds or note-management features.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose notes will be retrieved.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// A read-only list of <see cref="TaskNote"/> entities created by the specified user.
        /// </returns>
        Task<IReadOnlyList<TaskNote>> ListByUserIdAsync(
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves a note by its unique identifier without enabling EF Core tracking.
        /// Use this for read-only scenarios or validation workflows that do not require mutation.
        /// </summary>
        /// <param name="noteId">The unique identifier of the note to retrieve.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The <see cref="TaskNote"/> entity, or <c>null</c> if no matching note is found.
        /// </returns>
        Task<TaskNote?> GetByIdAsync(
            Guid noteId,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves a note by its unique identifier with EF Core tracking enabled.
        /// Use this when intending to update the note so EF Core can detect changes
        /// and produce minimal update statements.
        /// </summary>
        /// <param name="noteId">The unique identifier of the note to retrieve.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The tracked <see cref="TaskNote"/> entity, or <c>null</c> if no matching note exists.
        /// </returns>
        Task<TaskNote?> GetByIdForUpdateAsync(
            Guid noteId,
            CancellationToken ct = default);

        /// <summary>
        /// Adds a new <see cref="TaskNote"/> to the persistence context.
        /// </summary>
        /// <param name="note">The note entity to add.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task AddAsync(TaskNote note, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing <see cref="TaskNote"/> within the persistence context.
        /// EF Core change tracking will detect modifications and generate minimal UPDATE statements.
        /// </summary>
        /// <param name="note">The note entity with updated state.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task UpdateAsync(TaskNote note, CancellationToken ct = default);

        /// <summary>
        /// Removes a <see cref="TaskNote"/> entity from the persistence context.
        /// Actual deletion occurs upon committing the surrounding unit of work.
        /// </summary>
        /// <param name="note">The note entity to remove.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task RemoveAsync(TaskNote note, CancellationToken ct = default);
    }
}
