using Domain.Entities;
using Domain.Enums;

namespace Application.Projects.Abstractions
{
    public interface IProjectRepository
    {
        Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Project?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<Project>> GetAllByUserAsync(Guid userId, ProjectFilter? filter = null, CancellationToken ct = default);

        Task<bool> ExistsByNameAsync(Guid ownerId, string name, CancellationToken ct = default);

        Task AddAsync(Project project, CancellationToken ct = default);
        Task<DomainMutation> RenameAsync(Guid id, string newName, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
