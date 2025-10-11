using Application.ProjectMembers.Abstractions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Api.Auth.Authorization
{
    public sealed class ProjectRoleAuthorizationHandler(IHttpContextAccessor http, IProjectMemberReadService membership)
        : AuthorizationHandler<ProjectRoleRequirement>
    {
        private readonly IHttpContextAccessor _http = http;
        private readonly IProjectMemberReadService _membership = membership;

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ProjectRoleRequirement requirement)
        {
            var httpContext = _http.HttpContext;
            if (httpContext is null) return;

            // userId from claims
            var sub = context.User.FindFirst("sub") ?? context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (sub is null || !Guid.TryParse(sub.Value, out var userId)) return;

            // projectId from route values
            var routeVals = httpContext.GetRouteData()?.Values;
            if (routeVals is null ||
                !routeVals.TryGetValue("projectId", out var raw) ||
                raw is null ||
                !Guid.TryParse(raw.ToString(), out var projectId)) return;

            var role = await _membership.GetRoleAsync(projectId, userId, httpContext.RequestAborted);
            if (role is null) return;

            if (role.Value >= requirement.MinimumRole)
                context.Succeed(requirement);
        }
    }
}
