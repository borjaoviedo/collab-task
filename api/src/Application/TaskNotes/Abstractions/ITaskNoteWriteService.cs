using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskNotes.Abstractions
{
    public interface ITaskNoteWriteService
    {
        Task<(DomainMutation, TaskNote?)> CreateAsync(
            Guid projectId,
            Guid taskId,
            Guid userId,
            NoteContent content,
            CancellationToken ct = default);
        Task<DomainMutation> EditAsync(
            Guid projectId,
            Guid taskId,
            Guid noteId,
            Guid userId,
            NoteContent content,
            byte[] rowVersion,
            CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(
            Guid projectId,
            Guid noteId,
            Guid userId,
            byte[] rowVersion,
            CancellationToken ct = default);
    }
}
