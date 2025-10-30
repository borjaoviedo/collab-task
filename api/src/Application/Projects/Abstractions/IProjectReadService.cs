using Application.Projects.Filters;
using Domain.Entities;

namespace Application.Projects.Abstractions
{
    /// <summary>
    /// Provides read-only access to projects owned or joined by users.
    /// </summary>
    public interface IProjectReadService
    {
        /// <summary>Retrieves a project by its unique identifier.</summary>
        Task<Project?> GetAsync(Guid projectId, CancellationToken ct = default);

        /// <summary>
        /// Lists projects associated with a user, optionally filtered by state or visibility.
        /// </summary>
        Task<IReadOnlyList<Project>> ListByUserAsync(
            Guid userId,
            ProjectFilter? filter = null,
            CancellationToken ct = default);
    }
}
