using Application.ProjectMembers.Abstractions;
using Domain.Entities;
using Domain.Enums;

namespace Application.ProjectMembers.Services
{
    public sealed class ProjectMemberWriteService(IProjectMemberRepository repo) : IProjectMemberWriteService
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
            await repo.SaveChangesAsync(ct);

            return (DomainMutation.Created, projectMember);
        }

        public async Task<DomainMutation> ChangeRoleAsync(
            Guid projectId,
            Guid userId,
            ProjectRole newRole,
            byte[] rowVersion,
            CancellationToken ct = default)
            => await repo.UpdateRoleAsync(projectId, userId, newRole, rowVersion, ct);

        public async Task<DomainMutation> RemoveAsync(Guid projectId, Guid userId, byte[] rowVersion, CancellationToken ct = default)
            => await repo.SetRemovedAsync(projectId, userId, rowVersion, ct);

        public async Task<DomainMutation> RestoreAsync(Guid projectId, Guid userId, byte[] rowVersion, CancellationToken ct = default)
            => await repo.SetRestoredAsync(projectId, userId, rowVersion, ct);
    }
}
