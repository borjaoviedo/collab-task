using Application.ProjectMembers.DTOs;

namespace Application.ProjectMembers.Abstractions
{
    /// <summary>
    /// Provides write operations for managing <see cref="ProjectMember"/> entities,
    /// including creation, role changes, soft-removal, and restoration of project members.
    /// All operations enforce project-membership invariants, role constraints,
    /// and optimistic concurrency via the application’s unit of work.
    /// Returned results are mapped to <see cref="ProjectMemberReadDto"/> to expose a
    /// consistent API-facing read model after each write operation.
    /// </summary>
    public interface IProjectMemberWriteService
    {
        /// <summary>
        /// Creates a new membership entry for the specified user within the given project.
        /// Throws <see cref="Common.Exceptions.ConflictException"/> when the user is already a member
        /// or when membership constraints prevent creation.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project where the membership will be created.</param>
        /// <param name="dto">The data required to create the project membership.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ProjectMemberReadDto"/> representing the newly created membership.
        /// </returns>
        Task<ProjectMemberReadDto> CreateAsync(
            Guid projectId,
            ProjectMemberCreateDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Updates the role of an existing project member.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the membership does not exist
        /// and <see cref="Common.Exceptions.ConflictException"/> when role constraints are violated.
        /// </summary>
        /// <param name="projectId">The identifier of the project whose membership will be updated.</param>
        /// <param name="userId">The identifier of the user whose role will be changed.</param>
        /// <param name="dto">The new role to assign to the user.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ProjectMemberReadDto"/> representing the updated membership.
        /// </returns>
        Task<ProjectMemberReadDto> ChangeRoleAsync(
            Guid projectId,
            Guid userId,
            ProjectMemberChangeRoleDto dto,
            CancellationToken ct = default);

        /// <summary>
        /// Marks an existing project member as removed (soft delete).
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the membership does not exist.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <param name="userId">The unique identifier of the user to remove.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ProjectMemberReadDto"/> describing the updated (removed) membership.
        /// </returns>
        Task<ProjectMemberReadDto> RemoveAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default);

        /// <summary>
        /// Restores a previously removed project member.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the membership does not exist
        /// or <see cref="Common.Exceptions.ConflictException"/> when restoration is not allowed.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <param name="userId">The unique identifier of the member to restore.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A <see cref="ProjectMemberReadDto"/> representing the restored membership.
        /// </returns>
        Task<ProjectMemberReadDto> RestoreAsync(
            Guid projectId,
            Guid userId,
            CancellationToken ct = default);
    }
}
