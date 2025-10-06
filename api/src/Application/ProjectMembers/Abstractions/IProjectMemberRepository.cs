using Domain.Entities;
using Domain.Enums;

namespace Application.ProjectMembers.Abstractions
{
    public interface IProjectMemberRepository
    {
        Task<ProjectMember?> GetAsync(Guid projectId, Guid userId, CancellationToken ct = default);
        Task<IReadOnlyList<ProjectMember>> GetByProjectAsync(Guid projectId, bool includeRemoved = false, CancellationToken ct = default);
        Task<bool> ExistsAsync(Guid projectId, Guid userId, CancellationToken ct = default);
        Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default);
        Task AddAsync(ProjectMember member, CancellationToken ct = default);
        Task<DomainMutation> UpdateRoleAsync(Guid projectId, Guid userId, ProjectRole newRole, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> SetRemovedAsync(Guid projectId, Guid userId, DateTimeOffset? removedAt, byte[] rowVersion, CancellationToken ct = default);
    }
}
