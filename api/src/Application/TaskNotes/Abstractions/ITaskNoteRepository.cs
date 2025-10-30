using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskNotes.Abstractions
{
    /// <summary>
    /// Defines persistence operations for task note entities.
    /// </summary>
    public interface ITaskNoteRepository
    {
        /// <summary>Lists all notes attached to a specific task.</summary>
        Task<IReadOnlyList<TaskNote>> ListByTaskAsync(Guid taskId, CancellationToken ct = default);

        /// <summary>Lists all notes created by a specific user.</summary>
        Task<IReadOnlyList<TaskNote>> ListByUserAsync(Guid userId, CancellationToken ct = default);

        /// <summary>Gets a note by its identifier without tracking.</summary>
        Task<TaskNote?> GetByIdAsync(Guid noteId, CancellationToken ct = default);

        /// <summary>Gets a note by its identifier with tracking enabled.</summary>
        Task<TaskNote?> GetTrackedByIdAsync(Guid noteId, CancellationToken ct = default);

        /// <summary>Adds a new note to the persistence context.</summary>
        Task AddAsync(TaskNote note, CancellationToken ct = default);

        /// <summary>Edits the content of an existing note enforcing concurrency.</summary>
        Task<PrecheckStatus> EditAsync(
            Guid noteId,
            NoteContent newContent,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Deletes a note if concurrency checks pass.</summary>
        Task<PrecheckStatus> DeleteAsync(
            Guid noteId,
            byte[] rowVersion,
            CancellationToken ct = default);
    }

}
