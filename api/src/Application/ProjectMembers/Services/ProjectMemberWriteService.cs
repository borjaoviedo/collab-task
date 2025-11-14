using Application.Abstractions.Extensions;
using Application.Abstractions.Persistence;
using Application.ProjectMembers.Abstractions;
using Domain.Entities;
using Domain.Enums;

namespace Application.ProjectMembers.Services
{
    /// <summary>
    /// Write-side application service for project members.
    /// </summary>
    public sealed class ProjectMemberWriteService(IProjectMemberRepository repo, IUnitOfWork uow) : IProjectMemberWriteService
    {
        /// <summary>
        /// Creates a new membership for a user within a project.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="userId">The user to add.</param>
        /// <param name="role">The initial role.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The mutation result and the created membership when successful.</returns>
        public async Task<(DomainMutation, ProjectMember?)> CreateAsync(
            Guid projectId,
            Guid userId,
            ProjectRole role,
            CancellationToken ct = default)
        {
            if (await repo.ExistsAsync(projectId, userId, ct)) return (DomainMutation.NotFound, null);

            var projectMember = ProjectMember.Create(projectId, userId, role);
            await repo.AddAsync(projectMember, ct);

            var createResult = await uow.SaveAsync(MutationKind.Create, ct);
            return (createResult, projectMember);
        }

        /// <summary>
        /// Changes the role of an existing project member with concurrency enforcement.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="newRole">The new role to assign.</param>
        /// <param name="rowVersion">Concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<DomainMutation> ChangeRoleAsync(
            Guid projectId,
            Guid userId,
            ProjectRole newRole,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var changeRole = await repo.UpdateRoleAsync(projectId, userId, newRole, rowVersion, ct);
            if (changeRole != PrecheckStatus.Ready) return changeRole.ToErrorDomainMutation();

            var updateResult = await uow.SaveAsync(MutationKind.Update, ct);
            return updateResult;
        }

        /// <summary>
        /// Soft-removes a project member with concurrency enforcement.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="rowVersion">Concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<DomainMutation> RemoveAsync(
            Guid projectId,
            Guid userId,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var remove = await repo.SetRemovedAsync(projectId, userId, rowVersion, ct);
            if (remove != PrecheckStatus.Ready) return remove.ToErrorDomainMutation();

            var updateResult = await uow.SaveAsync(MutationKind.Update, ct);
            return updateResult;
        }

        /// <summary>
        /// Restores a previously removed project member with concurrency enforcement.
        /// </summary>
        /// <param name="projectId">The project identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="rowVersion">Concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<DomainMutation> RestoreAsync(
            Guid projectId,
            Guid userId,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var restore = await repo.SetRestoredAsync(projectId, userId, rowVersion, ct);
            if (restore != PrecheckStatus.Ready) return restore.ToErrorDomainMutation();

            var updateResult = await uow.SaveAsync(MutationKind.Update, ct);
            return updateResult;
        }
    }
}
