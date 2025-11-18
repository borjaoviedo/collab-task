using Application.Projects.Filters;
using Domain.Entities;

namespace Application.Projects.Abstractions
{
    /// <summary>
    /// Defines persistence operations for <see cref="Project"/> entities.
    /// </summary>
    public interface IProjectRepository
    {
        /// <summary>
        /// Retrieves all projects associated with a given user, optionally filtered
        /// by visibility or state.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose projects will be retrieved.</param>
        /// <param name="filter">
        /// Optional filter specifying project visibility, state, or other query constraints.
        /// </param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// A read-only list of <see cref="Project"/> entities associated with the specified user.
        /// </returns>
        Task<IReadOnlyList<Project>> ListByUserIdAsync(
            Guid userId,
            ProjectFilter? filter = null,
            CancellationToken ct = default);

        /// <summary>
        /// Retrieves a project by its unique identifier.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project to retrieve.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The <see cref="Project"/> entity, or <c>null</c> if no matching project is found.
        /// </returns>
        Task<Project?> GetByIdAsync(Guid projectId, CancellationToken ct = default);

        /// <summary>
        /// Retrieves a <see cref="Project"/> aggregate for update with EF Core tracking enabled.
        /// Use this when you plan to mutate the aggregate so EF can detect modified columns
        /// and persist minimal updates without requiring an explicit <c>Update(entity)</c> call.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project to retrieve.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// The tracked <see cref="Project"/> entity, or <c>null</c> if no matching project is found.
        /// </returns>
        Task<Project?> GetByIdForUpdateAsync(Guid projectId, CancellationToken ct = default);

        /// <summary>
        /// Determines whether a project with the specified name already exists.
        /// This is typically used to enforce uniqueness constraints before creation
        /// or renaming operations.
        /// </summary>
        /// <param name="name">The name of the project to check for existence.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// <c>true</c> if a project with the given name exists; otherwise <c>false</c>.
        /// </returns>
        Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);

        /// <summary>
        /// Adds a new project entity to the persistence context.
        /// </summary>
        /// <param name="project">The project entity to add.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task AddAsync(Project project, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing project entity within the persistence context.
        /// </summary>
        /// <param name="project">The project entity with modified state.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task UpdateAsync(Project project, CancellationToken ct = default);

        /// <summary>
        /// Removes a project entity from the persistence context.
        /// </summary>
        /// <param name="project">The project entity to remove.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task RemoveAsync(Project project, CancellationToken ct = default);
    }
}
