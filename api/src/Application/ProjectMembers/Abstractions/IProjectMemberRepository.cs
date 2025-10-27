using Domain.Entities;
using Domain.Enums;

namespace Application.ProjectMembers.Abstractions
{
    public interface IProjectMemberRepository
    {
        Task<IReadOnlyList<ProjectMember>> ListByProjectAsync(
            Guid projectId,
            bool includeRemoved = false,
            CancellationToken ct = default);
        Task<ProjectMember?> GetByProjectAndUserIdAsync(Guid projectId, Guid userId, CancellationToken ct = default);
        Task<ProjectMember?> GetTrackedByProjectAndUserIdAsync(Guid projectId, Guid userId, CancellationToken ct = default);
        Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default);

        Task AddAsync(ProjectMember member, CancellationToken ct = default);

        Task<PrecheckStatus> UpdateRoleAsync(Guid projectId, Guid userId, ProjectRole newRole, byte[] rowVersion, CancellationToken ct = default);
        Task<PrecheckStatus> SetRemovedAsync(Guid projectId, Guid userId, byte[] rowVersion, CancellationToken ct = default);
        Task<PrecheckStatus> SetRestoredAsync(Guid projectId, Guid userId, byte[] rowVersion, CancellationToken ct = default);

        Task<bool> ExistsAsync(Guid projectId, Guid userId, CancellationToken ct = default);
        Task<int> CountUserActiveMembershipsAsync(Guid userId, CancellationToken ct = default);
    }
}
