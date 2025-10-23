using Application.TaskNotes.Abstractions;
using Domain.Entities;

namespace Application.TaskNotes.Services
{
    public sealed class TaskNoteReadService(ITaskNoteRepository repo) : ITaskNoteReadService
    {
        public async Task<TaskNote?> GetAsync(Guid noteId, CancellationToken ct = default)
            => await repo.GetByIdAsync(noteId, ct);

        public async Task<IReadOnlyList<TaskNote>> ListByTaskAsync(Guid taskId, CancellationToken ct = default)
            => await repo.ListByTaskAsync(taskId, ct);

        public async Task<IReadOnlyList<TaskNote>> ListByUserAsync(Guid userId, CancellationToken ct = default)
            => await repo.ListByAuthorAsync(userId, ct);
    }
}
