using Domain.Entities;

namespace Application.TaskNotes.Abstractions
{
    /// <summary>
    /// Provides read-only access to task notes associated with tasks or users.
    /// </summary>
    public interface ITaskNoteReadService
    {
        /// <summary>Retrieves a note by its unique identifier.</summary>
        Task<TaskNote?> GetAsync(Guid noteId, CancellationToken ct = default);

        /// <summary>Lists all notes belonging to a specific task.</summary>
        Task<IReadOnlyList<TaskNote>> ListByTaskAsync(Guid taskId, CancellationToken ct = default);

        /// <summary>Lists all notes created by a specific user.</summary>
        Task<IReadOnlyList<TaskNote>> ListByUserAsync(Guid userId, CancellationToken ct = default);
    }
}
