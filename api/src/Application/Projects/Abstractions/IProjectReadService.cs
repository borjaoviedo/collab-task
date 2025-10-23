using Domain.Entities;

namespace Application.Projects.Abstractions
{
    public interface IProjectReadService
    {
        Task<Project?> GetAsync(Guid projectId, CancellationToken ct = default);
        Task<IReadOnlyList<Project>> ListByUserAsync(Guid userId, ProjectFilter? filter = null, CancellationToken ct = default);
    }
}
