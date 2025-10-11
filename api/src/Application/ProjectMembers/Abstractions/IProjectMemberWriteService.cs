using Domain.Entities;
using Domain.Enums;

namespace Application.ProjectMembers.Abstractions
{
    public interface IProjectMemberWriteService
    {
        Task<(DomainMutation, ProjectMember?)> CreateAsync(Guid projectId, Guid userId, ProjectRole role, DateTimeOffset joinedAt, CancellationToken ct = default);
        Task<DomainMutation> ChangeRoleAsync(Guid projectId, Guid userId, ProjectRole newRole, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> RemoveAsync(Guid projectId, Guid userId, byte[] rowVersion, DateTimeOffset removedAt, CancellationToken ct = default);
        Task<DomainMutation> RestoreAsync(Guid projectId, Guid userId, byte[] rowVersion, CancellationToken ct = default);
    }
}
