using Application.TaskNotes.Abstractions;
using Domain.Entities;

namespace Application.TaskNotes.Services
{
    /// <summary>
    /// Read-only application service for task notes.
    /// </summary>
    public sealed class TaskNoteReadService(ITaskNoteRepository repo) : ITaskNoteReadService
    {
        /// <summary>Retrieves a note by its identifier.</summary>
        /// <param name="noteId">The note identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The note if found; otherwise <c>null</c>.</returns>
        public async Task<TaskNote?> GetAsync(Guid noteId, CancellationToken ct = default)
            => await repo.GetByIdAsync(noteId, ct);

        /// <summary>Lists all notes belonging to a specific task.</summary>
        /// <param name="taskId">The task identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<IReadOnlyList<TaskNote>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => await repo.ListByTaskAsync(taskId, ct);

        /// <summary>Lists all notes created by a specific user.</summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<IReadOnlyList<TaskNote>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => await repo.ListByUserAsync(userId, ct);
    }

}
