using Application.ProjectMembers.DTOs;
using Domain.Entities;

namespace Application.ProjectMembers.Abstractions
{
    /// <summary>
    /// Provides read-only access to <see cref="ProjectMember"/> entities,
    /// exposing query operations for retrieving membership information,
    /// user roles within a project, and aggregate membership statistics.
    /// This service is used by higher-level application features such as
    /// authorization checks, membership validation, and project-user analytics.
    /// </summary>
    public interface IProjectMemberReadService
    {
        /// <summary>
        /// Retrieves a membership record for a specific project–user pair.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the membership does not exist.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ProjectMemberReadDto"/> representing the membership entry.
        /// </returns>
        Task<ProjectMemberReadDto> GetByProjectAndUserIdAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Lists all members of the specified project.
        /// Removed (soft-deleted) members may be included when explicitly requested.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <param name="includeRemoved">
        /// Whether removed members should be included in the result set.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A read-only list of <see cref="ProjectMemberReadDto"/> objects associated with the project.
        /// </returns>
        Task<IReadOnlyList<ProjectMemberReadDto>> ListByProjectIdAsync(
            Guid projectId,
            bool includeRemoved = false,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves the role assigned to a user within the specified project.
        /// Returns <c>null</c> when the user is not a project member.
        /// </summary>
        /// <param name="projectId">The project whose role membership is being queried.</param>
        /// <param name="userId">The user whose project role will be retrieved.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ProjectMemberRoleReadDto"/> value if the user belongs to the project; otherwise <c>null</c>.
        /// </returns>
        Task<ProjectMemberRoleReadDto> GetUserRoleAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Counts the number of active (non-removed) project memberships
        /// associated with the specified user.
        /// Useful for analytics, quota enforcement, dashboards, or usage-based features.
        /// </summary>
        /// <param name="userId">
        /// The unique identifier of the user whose active memberships will be counted.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ProjectMemberCountReadDto"/> containing the number of
        /// active project memberships associated with the specified user.
        /// </returns>
        Task<ProjectMemberCountReadDto> CountActiveUsersAsync(
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Counts the number of active (non-removed) project memberships
        /// associated with the currently authenticated user.
        /// Useful for analytics, quota enforcement, dashboards, or usage-based features.
        /// Throws <see cref="UnauthorizedAccessException"/> when no user is authenticated.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ProjectMemberCountReadDto"/> containing the number of
        /// active project memberships associated with the authenticated user.
        /// </returns>
        Task<ProjectMemberCountReadDto> CountActiveSelfAsync(
            CancellationToken ct = default);
    }
}
