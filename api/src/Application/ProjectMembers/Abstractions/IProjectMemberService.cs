using Application.Common.Results;
using Domain.Enums;

namespace Application.ProjectMembers.Abstractions
{
    public interface IProjectMemberService
    {
        Task<WriteResult> AddAsync(Guid projectId, Guid userId, ProjectRole role, DateTimeOffset joinedAt, CancellationToken ct);
        Task<WriteResult> ChangeRoleAsync(Guid projectId, Guid userId, ProjectRole newRole, byte[] rowVersion, CancellationToken ct);
        Task<WriteResult> RemoveAsync(Guid projectId, Guid userId, byte[] rowVersion, DateTimeOffset removedAt, CancellationToken ct);
        Task<WriteResult> RestoreAsync(Guid projectId, Guid userId, byte[] rowVersion, CancellationToken ct);
    }
}
