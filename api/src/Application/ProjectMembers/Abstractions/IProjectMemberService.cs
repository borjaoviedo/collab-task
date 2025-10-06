using Application.Common.Results;
using Domain.Enums;

namespace Application.ProjectMembers.Abstractions
{
    public interface IProjectMemberService
    {
        Task<WriteResult> UpdateRoleAsync(Guid projectId, Guid userId, ProjectRole role, byte[] rowVersion, CancellationToken ct = default);
        Task<WriteResult> RemoveAsync(Guid projectId, Guid userId, DateTimeOffset? removedAt, byte[] rowVersion, CancellationToken ct = default);
    }
}
