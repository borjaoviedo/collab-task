using Application.Common.Abstractions.Extensions;
using Application.Common.Abstractions.Persistence;
using Application.Projects.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Projects.Services
{
    public sealed class ProjectWriteService(IProjectRepository repo, IUnitOfWork uow) : IProjectWriteService
    {
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
