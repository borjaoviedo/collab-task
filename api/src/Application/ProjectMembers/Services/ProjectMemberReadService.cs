using Application.ProjectMembers.Abstractions;
using Domain.Entities;
using Domain.Enums;

namespace Application.ProjectMembers.Services
{
    public sealed class ProjectMemberReadService(IProjectMemberRepository repo) : IProjectMemberReadService
    {
        public async Task<ProjectMember?> GetAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => await repo.GetByProjectAndUserIdAsync(projectId, userId, ct);

        public async Task<IReadOnlyList<ProjectMember>> ListByProjectAsync(Guid projectId, bool includeRemoved = false, CancellationToken ct = default)
            => await repo.ListByProjectAsync(projectId, includeRemoved, ct);

        public async Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => await repo.GetRoleAsync(projectId, userId, ct);

        public async Task<int> CountActiveAsync(Guid userId, CancellationToken ct = default)
            => await repo.CountUserActiveMembershipsAsync(userId, ct);
    }
}
