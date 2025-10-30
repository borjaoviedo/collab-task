using Domain.Entities;
using Domain.Enums;

namespace Application.ProjectMembers.Abstractions
{
    /// <summary>
    /// Defines persistence operations for project member entities.
    /// </summary>
    public interface IProjectMemberRepository
    {
        /// <summary>Lists members of a project with optional inclusion of removed ones.</summary>
        Task<IReadOnlyList<ProjectMember>> ListByProjectAsync(
            Guid projectId,
            bool includeRemoved = false,
            CancellationToken ct = default);

        /// <summary>Gets a project member by project and user IDs without tracking.</summary>
        Task<ProjectMember?> GetByProjectAndUserIdAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default);

        /// <summary>Gets a project member by project and user IDs with tracking enabled.</summary>
        Task<ProjectMember?> GetTrackedByProjectAndUserIdAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default);

        /// <summary>Gets the role of a user within a specific project.</summary>
        Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default);

        /// <summary>Adds a new project member to the persistence context.</summary>
        Task AddAsync(ProjectMember member, CancellationToken ct = default);

        /// <summary>Updates the role of a project member with concurrency enforcement.</summary>
        Task<PrecheckStatus> UpdateRoleAsync(
            Guid projectId,
            Guid userId,
            ProjectRole newRole,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Marks a project member as removed.</summary>
        Task<PrecheckStatus> SetRemovedAsync(
            Guid projectId,
            Guid userId,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Restores a previously removed project member.</summary>
        Task<PrecheckStatus> SetRestoredAsync(
            Guid projectId,
            Guid userId,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Checks if a user is already a member of a given project.</summary>
        Task<bool> ExistsAsync(Guid projectId, Guid userId, CancellationToken ct = default);

        /// <summary>Counts the number of active memberships a user currently holds.</summary>
        Task<int> CountUserActiveMembershipsAsync(Guid userId, CancellationToken ct = default);
    }
}
