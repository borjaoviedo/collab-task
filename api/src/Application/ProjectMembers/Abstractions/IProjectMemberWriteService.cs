using Domain.Entities;
using Domain.Enums;

namespace Application.ProjectMembers.Abstractions
{
    /// <summary>
    /// Handles project membership commands at the application layer.
    /// </summary>
    public interface IProjectMemberWriteService
    {
        /// <summary>Creates a new membership for a user within a project.</summary>
        Task<(DomainMutation, ProjectMember?)> CreateAsync(
            Guid projectId,
            Guid userId,
            ProjectRole role,
            CancellationToken ct = default);

        /// <summary>Changes the role of an existing member.</summary>
        Task<DomainMutation> ChangeRoleAsync(
            Guid projectId,
            Guid userId,
            ProjectRole newRole,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Marks a project member as removed.</summary>
        Task<DomainMutation> RemoveAsync(
            Guid projectId,
            Guid userId,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Restores a previously removed member.</summary>
        Task<DomainMutation> RestoreAsync(
            Guid projectId,
            Guid userId,
            byte[] rowVersion,
            CancellationToken ct = default);
    }
}
