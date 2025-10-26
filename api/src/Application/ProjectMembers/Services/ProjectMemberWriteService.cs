using Application.Common.Abstractions.Extensions;
using Application.Common.Abstractions.Persistence;
using Application.ProjectMembers.Abstractions;
using Domain.Entities;
using Domain.Enums;

namespace Application.ProjectMembers.Services
{
    public sealed class ProjectMemberWriteService(IProjectMemberRepository repo, IUnitOfWork uow) : IProjectMemberWriteService
    {
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
