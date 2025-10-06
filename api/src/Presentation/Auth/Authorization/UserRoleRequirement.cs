using Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Api.Auth.Authorization
{
    public sealed class UserRoleRequirement : IAuthorizationRequirement
    {
        public UserRoleRequirement(UserRole minimumRole) => MinimumRole = minimumRole;
        public UserRole MinimumRole { get; }
    }
}
