using Domain.Entities;
using Domain.Enums;

namespace Application.ProjectMembers.Abstractions
{
    public interface IProjectMemberReadService
    {
        Task<ProjectMember?> GetAsync(Guid projectId, Guid userId, CancellationToken ct = default);
        Task<IReadOnlyList<ProjectMember>> ListByProjectAsync(Guid projectId, bool includeRemoved = false, CancellationToken ct = default);
        Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default);
        Task<int> CountActiveAsync(Guid userId, CancellationToken ct = default);
    }
}
