using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Api.Auth.Authorization
{
    public sealed class UserRoleAuthorizationHandler : AuthorizationHandler<UserRoleRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            UserRoleRequirement requirement)
        {
            var roleClaim = context.User.FindFirst(ClaimTypes.Role) ?? context.User.FindFirst("role");
            if (roleClaim is null) return Task.CompletedTask;

            if (!Enum.TryParse<UserRole>(roleClaim.Value, ignoreCase: true, out var userRole))
                return Task.CompletedTask;

            if (userRole >= requirement.MinimumRole)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
