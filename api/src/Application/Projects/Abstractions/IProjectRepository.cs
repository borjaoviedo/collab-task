using Application.Projects.Filters;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Projects.Abstractions
{
    /// <summary>
    /// Defines persistence operations for project entities.
    /// </summary>
    public interface IProjectRepository
    {
        /// <summary>Lists projects owned or joined by a user.</summary>
        Task<IReadOnlyList<Project>> ListByUserAsync(
            Guid userId,
            ProjectFilter? filter = null,
            CancellationToken ct = default);

        /// <summary>Gets a project by its identifier without tracking.</summary>
        Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>Gets a project by its identifier with change tracking enabled.</summary>
        Task<Project?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>Adds a new project to the persistence context.</summary>
        Task AddAsync(Project project, CancellationToken ct = default);

        /// <summary>Renames a project enforcing concurrency via row version.</summary>
        Task<PrecheckStatus> RenameAsync(
            Guid id,
            ProjectName newName,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Deletes a project if concurrency and constraints allow it.</summary>
        Task<PrecheckStatus> DeleteAsync(
            Guid id,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Checks if a project name already exists for the same owner.</summary>
        Task<bool> ExistsByNameAsync(
            Guid ownerId,
            ProjectName name,
            CancellationToken ct = default);
    }
}
