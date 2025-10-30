using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskNotes.Abstractions
{
    /// <summary>
    /// Handles creation, editing, and deletion of task notes at the application level.
    /// </summary>
    public interface ITaskNoteWriteService
    {
        /// <summary>Creates a new note for a given task and user.</summary>
        Task<(DomainMutation, TaskNote?)> CreateAsync(
            Guid projectId,
            Guid taskId,
            Guid userId,
            NoteContent content,
            CancellationToken ct = default);

        /// <summary>Edits the content of an existing note.</summary>
        Task<DomainMutation> EditAsync(
            Guid projectId,
            Guid taskId,
            Guid noteId,
            Guid userId,
            NoteContent content,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Deletes an existing note.</summary>
        Task<DomainMutation> DeleteAsync(
            Guid projectId,
            Guid noteId,
            Guid userId,
            byte[] rowVersion,
            CancellationToken ct = default);
    }

}
