using Domain.Entities;

namespace Application.Projects.Abstractions
{
    public interface IProjectReadService
    {
        Task<Project?> GetAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<Project>> GetAllByUserAsync(Guid userId, ProjectFilter? filter = null, CancellationToken ct = default);
    }
}
