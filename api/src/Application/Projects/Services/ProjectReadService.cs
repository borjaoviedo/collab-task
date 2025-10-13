using Application.Projects.Abstractions;
using Domain.Entities;

namespace Application.Projects.Services
{
    public sealed class ProjectReadService(IProjectRepository repo) : IProjectReadService
    {
        public async Task<Project?> GetAsync(Guid id, CancellationToken ct = default)
            => await repo.GetByIdAsync(id, ct);

        public async Task<IReadOnlyList<Project>> GetAllByUserAsync(Guid userId, ProjectFilter? filter = null, CancellationToken ct = default)
            => await repo.GetAllByUserAsync(userId, filter, ct);
    }
}
