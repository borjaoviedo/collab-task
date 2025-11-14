using Application.Abstractions.Extensions;
using Application.Abstractions.Persistence;
using Application.Projects.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Projects.Services
{
    /// <summary>
    /// Write-side application service for projects.
    /// </summary>
    public sealed class ProjectWriteService(IProjectRepository repo, IUnitOfWork uow) : IProjectWriteService
    {
        /// <summary>
        /// Creates a new project for the specified user.
        /// </summary>
        /// <param name="userId">The owner user identifier.</param>
        /// <param name="name">The project name.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The mutation result and the created project when successful.</returns>
        public async Task<(DomainMutation, Project?)> CreateAsync(
            Guid userId,
            ProjectName name,
            CancellationToken ct = default)
        {
            if (await repo.ExistsByNameAsync(userId, name, ct)) return (DomainMutation.Conflict, null);

            var project = Project.Create(userId, name);
            await repo.AddAsync(project, ct);

            var createResult = await uow.SaveAsync(MutationKind.Create, ct);
            return (createResult, project);
        }

        /// <summary>
        /// Renames an existing project with concurrency enforcement.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="newName">The new project name.</param>
        /// <param name="rowVersion">Concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<DomainMutation> RenameAsync(
            Guid projectId,
            ProjectName newName,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var rename = await repo.RenameAsync(projectId, newName, rowVersion, ct);
            if (rename != PrecheckStatus.Ready) return rename.ToErrorDomainMutation();

            var updateResult = await uow.SaveAsync(MutationKind.Update, ct);
            return updateResult;
        }

        /// <summary>
        /// Deletes a project with concurrency enforcement.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="rowVersion">Concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<DomainMutation> DeleteAsync(
            Guid projectId,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var delete = await repo.DeleteAsync(projectId, rowVersion, ct);
            if (delete != PrecheckStatus.Ready) return delete.ToErrorDomainMutation();

            var deleteResult = await uow.SaveAsync(MutationKind.Delete, ct);
            return deleteResult;
        }
    }

}
