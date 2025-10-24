using Domain.Entities;

namespace Application.TaskNotes.Abstractions
{
    public interface ITaskNoteReadService
    {
        Task<TaskNote?> GetAsync(Guid noteId, CancellationToken ct = default);
        Task<IReadOnlyList<TaskNote>> ListByTaskAsync(Guid taskId, CancellationToken ct = default);
        Task<IReadOnlyList<TaskNote>> ListByUserAsync(Guid userId, CancellationToken ct = default);
    }
}
