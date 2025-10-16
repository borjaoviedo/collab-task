using Domain.Entities;
using Domain.Enums;

namespace Application.TaskNotes.Abstractions
{
    public interface ITaskNoteRepository
    {
        Task<TaskNote?> GetByIdAsync(Guid noteId, CancellationToken ct = default);
        Task<TaskNote?> GetTrackedByIdAsync(Guid noteId, CancellationToken ct = default);
        Task<IReadOnlyList<TaskNote>> ListByTaskAsync(Guid taskId, CancellationToken ct = default);
        Task<IReadOnlyList<TaskNote>> ListByAuthorAsync(Guid authorId, CancellationToken ct = default);

        Task AddAsync(TaskNote note, CancellationToken ct = default);
        Task<DomainMutation> EditAsync(Guid noteId, string newContent, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(Guid noteId, byte[] rowVersion, CancellationToken ct = default);

        Task<int> SaveCreateChangesAsync(CancellationToken ct = default);
        Task<DomainMutation> SaveUpdateChangesAsync(CancellationToken ct = default);
    }
}
