using Application.ProjectMembers.Abstractions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Api.Auth.Authorization
{
    /// <summary>
    /// Evaluates <see cref="ProjectRoleRequirement"/> against the current user on the routed project.
    /// </summary>
    /// <remarks>
    /// - Extracts userId from claims.
    /// - Resolves projectId from {projectId} route value.
    /// - Calls membership to check role >= required.
    /// Returns success only if the user belongs to the project and meets the minimum role.
    /// </remarks>
    public sealed class ProjectRoleAuthorizationHandler(IProjectMemberReadService membership) : AuthorizationHandler<ProjectRoleRequirement>
    {
        private readonly IProjectMemberReadService _membership = membership;

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ProjectRoleRequirement requirement)
        {
            // policy not applicable
            if (context.Resource is not HttpContext http) return;

            // userId from claims
            var sub = context.User.FindFirst("sub") ?? context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (sub is null || !Guid.TryParse(sub.Value, out var userId))
            {
                context.Fail(); 
                return;
            }

            // projectId from route values (must be present for this policy)
            if (!TryGetProjectId(http, out var projectId)) return;

            // membership
            var role = await _membership.GetRoleAsync(projectId, userId, http.RequestAborted);
            if (role is null)
            {
                context.Fail(); // 403
                return;
            }

            if (role.Value >= requirement.MinimumRole) context.Succeed(requirement);
            else context.Fail();
        }

        // Reads {projectId} from route data; returns false when absent or invalid.
        private static bool TryGetProjectId(HttpContext http, out Guid projectId)
        {
            projectId = Guid.Empty;
            var rv = http.GetRouteData()?.Values;
            if (rv is null) return false;

            if (rv.TryGetValue("projectId", out var raw) &&
                raw is not null &&
                Guid.TryParse(raw.ToString(), out projectId))
            {
                return true;
            }
            return false;
        }
    }
}
