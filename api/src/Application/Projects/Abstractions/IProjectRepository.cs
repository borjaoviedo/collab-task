using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Projects.Abstractions
{
    public interface IProjectRepository
    {
        Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<Project>> GetByUserAsync(Guid userId, ProjectFilter? filter = null, CancellationToken ct = default);
        Task AddAsync(Project project, CancellationToken ct = default);
        Task UpdateAsync(Project project, CancellationToken ct = default);
        Task DeleteAsync(Project project, CancellationToken ct = default);
        Task<bool> ExistsByNameAsync(Guid ownerId, ProjectName name, CancellationToken ct = default);
    }
}
