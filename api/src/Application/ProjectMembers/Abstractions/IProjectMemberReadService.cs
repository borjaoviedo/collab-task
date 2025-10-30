using Domain.Entities;
using Domain.Enums;

namespace Application.ProjectMembers.Abstractions
{
    /// <summary>
    /// Provides read-only access to project members and their roles.
    /// </summary>
    public interface IProjectMemberReadService
    {
        /// <summary>Gets a project member by project and user identifiers.</summary>
        Task<ProjectMember?> GetAsync(Guid projectId, Guid userId, CancellationToken ct = default);

        /// <summary>Lists members of a project, optionally including removed ones.</summary>
        Task<IReadOnlyList<ProjectMember>> ListByProjectAsync(
            Guid projectId,
            bool includeRemoved = false,
            CancellationToken ct = default);

        /// <summary>Retrieves the role of a user within a project.</summary>
        Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default);

        /// <summary>Counts how many active project memberships a user currently has.</summary>
        Task<int> CountActiveAsync(Guid userId, CancellationToken ct = default);
    }
}
