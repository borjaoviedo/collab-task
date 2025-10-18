using Domain.Enums;

namespace Application.ProjectMembers.DTOs
{
    public sealed class ProjectMemberChangeRoleDto
    {
        public required ProjectRole NewRole { get; set; }
    }
}
