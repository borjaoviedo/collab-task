using Application.Projects.DTOs;
using Application.Projects.Filters;

namespace Application.Projects.Abstractions
{
    /// <summary>
    /// Provides read-only access to project entities.
    /// </summary>
    public interface IProjectReadService
    {
        /// <summary>
        /// Retrieves a project by its unique identifier.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the project does not exist
        /// or is not accessible to the current user.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project to retrieve.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ProjectReadDto"/> representing the project.
        /// </returns>
        Task<ProjectReadDto> GetByIdAsync(Guid projectId, CancellationToken ct = default);

        /// <summary>
        /// Lists projects associated with a specific user, optionally filtered by visibility
        /// or state.
        /// </summary>
        /// <param name="userId">The identifier of the user whose projects will be listed.</param>
        /// <param name="filter">Optional filter for project visibility or state.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A read-only list of <see cref="ProjectReadDto"/> objects.
        /// </returns>
        Task<IReadOnlyList<ProjectReadDto>> ListByUserIdAsync(
            Guid userId,
            ProjectFilter? filter = null,
            CancellationToken ct = default);

        /// <summary>
        /// Lists projects associated with the currently authenticated user,
        /// optionally filtered by visibility or state.
        /// Throws <see cref="UnauthorizedAccessException"/> when no user is authenticated.
        /// </summary>
        /// <param name="filter">Optional filter for project visibility or state.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A read-only list of <see cref="ProjectReadDto"/> for the current user.
        /// </returns>
        Task<IReadOnlyList<ProjectReadDto>> ListSelfAsync(
            ProjectFilter? filter = null,
            CancellationToken ct = default);
    }
}
