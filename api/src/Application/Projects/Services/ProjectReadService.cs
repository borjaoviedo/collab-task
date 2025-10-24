using Application.Projects.Abstractions;
using Application.Projects.Filters;
using Domain.Entities;

namespace Application.Projects.Services
{
    public sealed class ProjectReadService(IProjectRepository repo) : IProjectReadService
    {
        public async Task<Project?> GetAsync(Guid projectId, CancellationToken ct = default)
            => await repo.GetByIdAsync(projectId, ct);

        public async Task<IReadOnlyList<Project>> ListByUserAsync(Guid userId, ProjectFilter? filter = null, CancellationToken ct = default)
            => await repo.GetAllByUserAsync(userId, filter, ct);
    }
}
