using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Projects.Abstractions
{
    /// <summary>
    /// Handles project creation and mutation commands at the application level.
    /// </summary>
    public interface IProjectWriteService
    {
        /// <summary>Creates a new project for a given user.</summary>
        Task<(DomainMutation, Project?)> CreateAsync(
            Guid userId,
            ProjectName name,
            CancellationToken ct = default);

        /// <summary>Renames an existing project.</summary>
        Task<DomainMutation> RenameAsync(
            Guid projectId,
            ProjectName newName,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Deletes an existing project.</summary>
        Task<DomainMutation> DeleteAsync(
            Guid projectId,
            byte[] rowVersion,
            CancellationToken ct = default);
    }
}
