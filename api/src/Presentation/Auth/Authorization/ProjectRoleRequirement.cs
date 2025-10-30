using Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Api.Auth.Authorization
{
    /// <summary>Minimum project role required to access a resource.</summary>
    /// <param name="minimumRole">Lowest role that satisfies the requirement.</param>
    public sealed class ProjectRoleRequirement(ProjectRole minimumRole) : IAuthorizationRequirement
    {
        public ProjectRole MinimumRole { get; } = minimumRole;
    }
}
