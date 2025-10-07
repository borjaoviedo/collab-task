using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Projects.Abstractions
{
    public interface IProjectRepository
    {
        Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<Project>> GetByUserAsync(Guid userId, ProjectFilter? filter = null, CancellationToken ct = default);
        Task AddAsync(Project project, CancellationToken ct = default);
        Task<DomainMutation> RenameAsync(Guid id, string newName, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default);
        Task<bool> ExistsByNameAsync(Guid ownerId, ProjectName name, CancellationToken ct = default);
    }
}
