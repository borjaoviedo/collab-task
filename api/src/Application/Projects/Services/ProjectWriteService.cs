using Application.Projects.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Projects.Services
{
    public sealed class ProjectWriteService(IProjectRepository repo) : IProjectWriteService
    {
        public async Task<(DomainMutation, Project?)> CreateAsync(Guid userId, ProjectName name, CancellationToken ct = default)
        {
            if (await repo.ExistsByNameAsync(userId, name, ct)) return (DomainMutation.Conflict, null);

            var project = Project.Create(userId, name);
            await repo.AddAsync(project, ct);
            await repo.SaveChangesAsync(ct);

            return (DomainMutation.Created, project);
        }

        public async Task<DomainMutation> RenameAsync(Guid projectId, ProjectName newName, byte[] rowVersion, CancellationToken ct = default)
            => await repo.RenameAsync(projectId, newName, rowVersion, ct);

        public async Task<DomainMutation> DeleteAsync(Guid projectId, byte[] rowVersion, CancellationToken ct = default)
            => await repo.DeleteAsync(projectId, rowVersion, ct);
    }
}
