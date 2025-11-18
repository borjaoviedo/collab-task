using Application.ProjectMembers.DTOs;
using Domain.Enums;

namespace TestHelpers.Api.Endpoints.Defaults
{
    public static class ProjectMemberDefaults
    {
        public readonly static Guid DefaultProjectMemberId = Guid.NewGuid();
        public readonly static ProjectRole DefaultProjectMemberRole = ProjectRole.Member;

        public readonly static ProjectMemberCreateDto DefaultProjectMemberCreateDto = new()
        {
            UserId = DefaultProjectMemberId,
            Role = DefaultProjectMemberRole
        };

        public readonly static ProjectRole DefaultProjectMemberChangeRole = ProjectRole.Admin;

        public readonly static ProjectMemberChangeRoleDto DefaultProjectMemberChangeRoleDto =
            new() { NewRole = DefaultProjectMemberChangeRole };
    }
}
