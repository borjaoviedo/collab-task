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
            var roles = context.User.Claims
             .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
             .Select(c => Enum.TryParse<UserRole>(c.Value, true, out var r) ? (UserRole?)r : null)
             .Where(r => r.HasValue)
             .Select(r => r!.Value);

            if (roles.Any() && roles.Max() >= requirement.MinimumRole)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
