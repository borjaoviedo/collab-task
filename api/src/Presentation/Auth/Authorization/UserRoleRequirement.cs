using Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace Api.Auth.Authorization
{
    public sealed class UserRoleRequirement(UserRole minimumRole) : IAuthorizationRequirement
    {
        public UserRole MinimumRole { get; } = minimumRole;
    }
}
