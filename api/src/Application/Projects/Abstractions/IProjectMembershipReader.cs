using Domain.Enums;

namespace Application.Projects.Abstractions
{
    public interface IProjectMembershipReader
    {
        Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default);
        Task<int> CountActiveAsync(Guid userId, CancellationToken ct = default);
    }
}
