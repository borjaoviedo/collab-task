using Domain.Entities;
using Domain.Enums;

namespace Application.ProjectMembers.Abstractions
{
    /// <summary>
    /// Defines persistence operations for <see cref="ProjectMember"/> entities,
    /// including membership lookup, role retrieval, existence checks, and updates.
    /// </summary>
    public interface IProjectMemberRepository
    {
        /// <summary>
        /// Retrieves all members of a given project, optionally including removed members.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project whose members will be retrieved.</param>
        /// <param name="includeRemoved">
        /// Whether to include members marked as removed (soft-deleted). Defaults to <c>false</c>.
        /// </param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// A read-only list of <see cref="ProjectMember"/> entities associated with the specified project.
        /// </returns>
        Task<IReadOnlyList<ProjectMember>> ListByProjectIdAsync(
            Guid projectId,
            bool includeRemoved = false,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves a specific membership by project and user identifier without enabling change tracking.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <param name="userId">The unique identifier of the user whose membership is requested.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The <see cref="ProjectMember"/> entity, or <c>null</c> if no membership is found.
        /// </returns>
        Task<ProjectMember?> GetByProjectAndUserIdAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves a membership by project and user identifier with EF Core tracking enabled.
        /// Use this when intending to modify the membership so EF Core can detect changes
        /// and generate minimal update statements.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The tracked <see cref="ProjectMember"/> entity, or <c>null</c> if no membership is found.
        /// </returns>
        Task<ProjectMember?> GetByProjectAndUserIdForUpdateAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves the role that a user holds within a given project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The <see cref="ProjectRole"/> assigned to the user within the project,
        /// or <c>null</c> if the user is not a member.
        /// </returns>
        Task<ProjectRole?> GetUserRoleAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Determines whether a membership already exists for the specified project and user.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// <c>true</c> if a membership exists; otherwise, <c>false</c>.
        /// </returns>
        Task<bool> ExistsAsync(Guid projectId, Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Counts the number of active (non-removed) project memberships assigned to a given user.
        /// Useful for enforcing limits or providing user analytics.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The number of active memberships the user currently holds.
        /// </returns>
        Task<int> CountUserActiveMembershipsAsync(Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Adds a new project membership to the persistence context.
        /// </summary>
        /// <param name="member">The membership entity to add.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task AddAsync(ProjectMember member, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing <see cref="ProjectMember"/> entity within the persistence context.
        /// </summary>
        /// <param name="member">The membership entity with modified state.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task UpdateAsync(ProjectMember member, CancellationToken ct = default);
    }
}
