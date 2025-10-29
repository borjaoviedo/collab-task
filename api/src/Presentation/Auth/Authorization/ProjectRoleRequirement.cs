using Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Api.Auth.Authorization
{
    public sealed class ProjectRoleRequirement(ProjectRole minimumRole) : IAuthorizationRequirement
    {
        public ProjectRole MinimumRole { get; } = minimumRole;
    }
}
