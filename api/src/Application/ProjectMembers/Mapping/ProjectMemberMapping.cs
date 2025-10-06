using Application.ProjectMembers.DTOs;
using Domain.Entities;

namespace Application.ProjectMembers.Mapping
{
    public static class ProjectMemberMapping
    {
        public static ProjectMemberReadDto ToReadDto(this ProjectMember item)
            => new()
            {
                ProjectId = item.ProjectId,
                UserId = item.UserId,
                UserName = item.User?.Name.Value ?? string.Empty,
                Role = item.Role,
                JoinedAt = item.JoinedAt,
                RemovedAt = item.RemovedAt,
                RowVersion = item.RowVersion
            };

        public static ProjectMember ToEntity(this ProjectMemberCreateDto dto, Guid projectId)
            => ProjectMember.Create(projectId, dto.UserId, dto.Role, dto.JoinedAt.ToUniversalTime());

        public static void ApplyRoleUpdate(this ProjectMember m, ProjectMemberUpdateRoleDto dto)
            => m.ChangeRole(dto.Role);

        public static void ApplyRemoval(this ProjectMember m, ProjectMemberRemoveDto dto, DateTimeOffset nowUtc)
            => m.Remove((dto.RemovedAt?.ToUniversalTime()) ?? nowUtc);
    }
}
