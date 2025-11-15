using Application.Projects.DTOs;

namespace Application.Projects.Abstractions
{
    /// <summary>
    /// Provides write operations for managing project entities,
    /// including creation, modification, and deletion.
    /// </summary>
    public interface IProjectWriteService
    {
        /// <summary>
        /// Creates a new project.
        /// </summary>
        /// <param name="dto">The data required to create the project.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ProjectReadDto"/> representing the newly created project.
        /// </returns>
        Task<ProjectReadDto> CreateAsync(
            ProjectCreateDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Renames an existing project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project to rename.</param>
        /// <param name="dto">The new name and related data.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ProjectReadDto"/> representing the updated project.
        /// </returns>
        Task<ProjectReadDto> RenameAsync(
            Guid projectId,
            ProjectRenameDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Deletes an existing project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project to delete.</param>
        /// <param name="ct">Cancellation token.</param>
        Task DeleteByIdAsync(
            Guid projectId,
            CancellationToken ct = default);
    }
}
