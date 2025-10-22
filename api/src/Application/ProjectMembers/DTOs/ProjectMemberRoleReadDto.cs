using Domain.Enums;

namespace Application.ProjectMembers.DTOs
{
    public sealed class ProjectMemberRoleReadDto
    {
        public required ProjectRole Role { get; init; }
    }
}
