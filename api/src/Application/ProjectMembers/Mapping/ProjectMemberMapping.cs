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
                Email = item.User?.Email.Value ?? string.Empty,
                Role = item.Role,
                JoinedAt = item.JoinedAt,
                RemovedAt = item.RemovedAt,
                RowVersion = item.RowVersion
            };
    }
}
