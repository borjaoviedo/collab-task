using Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Api.Auth.Authorization
{
    public sealed class ProjectRoleRequirement : IAuthorizationRequirement
    {
        public ProjectRoleRequirement(ProjectRole minimumRole) => MinimumRole = minimumRole;
        public ProjectRole MinimumRole { get; }
    }
}
