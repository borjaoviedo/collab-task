using Application.TaskNotes.DTOs;

namespace Application.TaskNotes.Abstractions
{
    /// <summary>
    /// Provides write operations for managing <see cref="TaskNote"/> entities,
    /// including creation, editing, and deletion of notes attached to tasks.
    /// All operations enforce project/task membership rules, validate note ownership,
    /// and produce DTOs that reflect the updated state when applicable.
    /// </summary>
    public interface ITaskNoteWriteService
    {
        /// <summary>
        /// Creates a new note associated with a given task and authored by the specified user.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the parent task does not exist.
        /// </summary>
        /// <param name="projectId">The identifier of the project containing the task.</param>
        /// <param name="taskId">The identifier of the task to which the note will be added.</param>
        /// <param name="userId">The identifier of the user creating the note.</param>
        /// <param name="dto">The data required to create the note.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="TaskNoteReadDto"/> representing the newly created note.
        /// </returns>
        Task<TaskNoteReadDto> CreateAsync(
            Guid projectId,
            Guid taskId,
            Guid userId,
            TaskNoteCreateDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Edits the content of an existing note.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the note does not exist
        /// or when the caller is not permitted to modify it.
        /// </summary>
        /// <param name="projectId">The identifier of the project containing the task.</param>
        /// <param name="taskId">The identifier of the task associated with the note.</param>
        /// <param name="noteId">The unique identifier of the note to edit.</param>
        /// <param name="userId">The identifier of the user performing the edit.</param>
        /// <param name="dto">The updated note content.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="TaskNoteReadDto"/> representing the updated note.
        /// </returns>
        Task<TaskNoteReadDto> EditAsync(
            Guid projectId,
            Guid taskId,
            Guid noteId,
            Guid userId,
            TaskNoteEditDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Deletes an existing note.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the note does not exist,
        /// or <see cref="Common.Exceptions.ConflictException"/> when deletion violates task or ownership constraints.
        /// </summary>
        /// <param name="projectId">The identifier of the project containing the task.</param>
        /// <param name="taskId">The identifier of the task containing the note.</param>
        /// <param name="noteId">The unique identifier of the note to delete.</param>
        /// <param name="userId">The identifier of the user initiating the deletion.</param>
        /// <param name="ct">Cancellation token.</param>
        Task DeleteAsync(
            Guid projectId,
            Guid taskId,
            Guid noteId,
            Guid userId,
            CancellationToken ct = default);
    }
}
