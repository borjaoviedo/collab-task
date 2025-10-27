using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.TaskNotes.Abstractions
{
    public interface ITaskNoteRepository
    {
        Task<IReadOnlyList<TaskNote>> ListByTaskAsync(Guid taskId, CancellationToken ct = default);
        Task<IReadOnlyList<TaskNote>> ListByUserAsync(Guid userId, CancellationToken ct = default);
        Task<TaskNote?> GetByIdAsync(Guid noteId, CancellationToken ct = default);
        Task<TaskNote?> GetTrackedByIdAsync(Guid noteId, CancellationToken ct = default);

        Task AddAsync(TaskNote note, CancellationToken ct = default);

        Task<PrecheckStatus> EditAsync(Guid noteId, NoteContent newContent, byte[] rowVersion, CancellationToken ct = default);
        Task<PrecheckStatus> DeleteAsync(Guid noteId, byte[] rowVersion, CancellationToken ct = default);
    }
}
