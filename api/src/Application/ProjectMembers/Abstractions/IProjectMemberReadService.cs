using Application.ProjectMembers.DTOs;
using Domain.Entities;
using Domain.Enums;

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
        /// A <see cref="ProjectRole"/> value if the user belongs to the project; otherwise <c>null</c>.
        /// </returns>
        Task<ProjectRole?> GetUserRoleAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Counts the number of active (non-removed) project memberships
        /// associated with the specified user.
        /// Useful for analytics, rate-limiting, or usage-based features.
        /// </summary>
        /// <param name="userId">The identifier of the user whose active memberships will be counted.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// The number of active project memberships held by the user.
        /// </returns>
        Task<int> CountActiveUsersAsync(Guid userId, CancellationToken ct = default);
    }
}
