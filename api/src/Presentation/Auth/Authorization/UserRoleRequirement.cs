using Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Api.Auth.Authorization
{
    /// <summary>Minimum user role required to access a resource.</summary>
    /// <param name="minimumRole">Lowest role that satisfies the requirement.</param>
    public sealed class UserRoleRequirement(UserRole minimumRole) : IAuthorizationRequirement
    {
        public UserRole MinimumRole { get; } = minimumRole;
    }
}
