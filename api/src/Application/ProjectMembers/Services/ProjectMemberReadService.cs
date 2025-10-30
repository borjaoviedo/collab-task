using Application.ProjectMembers.Abstractions;
using Domain.Entities;
using Domain.Enums;

namespace Application.ProjectMembers.Services
{
    /// <summary>
    /// Read-only application service for project members.
    /// </summary>
    public sealed class ProjectMemberReadService(IProjectMemberRepository repo) : IProjectMemberReadService
    {
        /// <summary>
        /// Retrieves a project member by project and user identifiers.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<ProjectMember?> GetAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => await repo.GetByProjectAndUserIdAsync(projectId, userId, ct);

        /// <summary>
        /// Lists members of a project, optionally including removed ones.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="includeRemoved">Whether to include soft-removed members.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<IReadOnlyList<ProjectMember>> ListByProjectAsync(
            Guid projectId,
            bool includeRemoved = false,
            CancellationToken ct = default)
            => await repo.ListByProjectAsync(projectId, includeRemoved, ct);

        /// <summary>
        /// Retrieves the role of a user within a project.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => await repo.GetRoleAsync(projectId, userId, ct);

        /// <summary>
        /// Counts the number of active memberships a user currently holds.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<int> CountActiveAsync(Guid userId, CancellationToken ct = default)
            => await repo.CountUserActiveMembershipsAsync(userId, ct);
    }
}
