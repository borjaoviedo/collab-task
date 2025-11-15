using Application.TaskNotes.DTOs;

namespace Application.TaskNotes.Abstractions
{
    /// <summary>
    /// Provides read-only access to <see cref="TaskNote"/> entities.
    /// Exposes query operations for retrieving individual notes,
    /// listing notes attached to a specific task, and listing notes authored
    /// by a given user. All returned values are projected to
    /// <see cref="TaskNoteReadDto"/> to provide a stable API-facing read model.
    /// </summary>
    public interface ITaskNoteReadService
    {
        /// <summary>
        /// Retrieves a task note by its unique identifier.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the note does not exist.
        /// </summary>
        /// <param name="noteId">The unique identifier of the note to retrieve.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="TaskNoteReadDto"/> describing the requested note.
        /// </returns>
        Task<TaskNoteReadDto> GetByIdAsync(Guid noteId, CancellationToken ct = default);

        /// <summary>
        /// Lists all notes attached to the specified task.
        /// Notes are typically ordered by creation time by the caller when building task timelines.
        /// </summary>
        /// <param name="taskId">The unique identifier of the task whose notes will be retrieved.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A read-only list of <see cref="TaskNoteReadDto"/> entries belonging to the task.
        /// </returns>
        Task<IReadOnlyList<TaskNoteReadDto>> ListByTaskIdAsync(Guid taskId, CancellationToken ct = default);

        /// <summary>
        /// Lists all notes authored by the specified user across all tasks.
        /// Useful for personal activity feeds, user dashboards, or auditing features.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose notes will be listed.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A read-only list of <see cref="TaskNoteReadDto"/> entries written by the user.
        /// </returns>
        Task<IReadOnlyList<TaskNoteReadDto>> ListByUserIdAsync(Guid userId, CancellationToken ct = default);
    }
}
