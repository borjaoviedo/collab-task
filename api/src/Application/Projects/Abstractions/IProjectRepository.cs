using Application.Projects.Filters;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Projects.Abstractions
{
    public interface IProjectRepository
    {
        Task<IReadOnlyList<Project>> GetAllByUserAsync(
            Guid userId,
            ProjectFilter? filter = null,
            CancellationToken ct = default);
        Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Project?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default);

        Task AddAsync(Project project, CancellationToken ct = default);

        Task<PrecheckStatus> RenameAsync(
            Guid id,
            ProjectName newName,
            byte[] rowVersion,
            CancellationToken ct = default);
        Task<PrecheckStatus> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default);

        Task<bool> ExistsByNameAsync(Guid ownerId, ProjectName name, CancellationToken ct = default);
    }
}
