using Application.Projects.Abstractions;
using Application.Projects.Filters;
using Domain.Entities;

namespace Application.Projects.Services
{
    /// <summary>
    /// Read-only application service for projects.
    /// </summary>
    public sealed class ProjectReadService(IProjectRepository repo) : IProjectReadService
    {
        /// <summary>
        /// Retrieves a project by identifier.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<Project?> GetAsync(Guid projectId, CancellationToken ct = default)
            => await repo.GetByIdAsync(projectId, ct);

        /// <summary>
        /// Lists projects associated with a user, using an optional filter.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="filter">Optional filtering criteria.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<IReadOnlyList<Project>> ListByUserAsync(
            Guid userId,
            ProjectFilter? filter = null,
            CancellationToken ct = default)
            => await repo.ListByUserAsync(userId, filter, ct);
    }

}
