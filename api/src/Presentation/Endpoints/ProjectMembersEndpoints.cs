using Api.Auth.Authorization;
using Api.Extensions;
using Api.Helpers;
using Application.ProjectMembers.Abstractions;
using Application.ProjectMembers.DTOs;
using Application.ProjectMembers.Mapping;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    public static class ProjectMembersEndpoints
    {
        public static RouteGroupBuilder MapProjectMembers(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/projects/{projectId:guid}/members")
                .WithTags("Project Members")
                .RequireAuthorization(Policies.ProjectReader);

            // GET /projects/{projectId}/members
            group.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromQuery] bool includeRemoved,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var members = await projectMemberReadSvc.ListByProjectAsync(projectId, includeRemoved, ct);
                var dto = members.Select(m => m.ToReadDto()).ToList();
                return Results.Ok(dto);
            })
            .Produces<IEnumerable<ProjectMemberReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get all members of a project")
            .WithDescription("Returns all members of the project.")
            .WithName("ProjectMembers_Get_All");

            // GET /projects/{projectId}/members/{userId}
            group.MapGet("/{userId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var pm = await projectMemberReadSvc.GetAsync(projectId, userId, ct);
                return pm is null ? Results.NotFound() : Results.Ok(pm.ToReadDto());
            })
            .Produces<ProjectMemberReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get project member")
            .WithDescription("Returns a project member by project and user id.")
            .WithName("ProjectMembers_Get");

            // GET /projects/{projectId}/members/{userId}/role
            group.MapGet("/{userId:guid}/role", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var role = await projectMemberReadSvc.GetRoleAsync(projectId, userId, ct);
                return role is null ? Results.NotFound() : Results.Ok(new { Role = role });
            })
            .Produces<object>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get member role")
            .WithDescription("Returns the role of a user within a project.")
            .WithName("ProjectMembers_GetRole");

            // POST /projects/{projectId}/members
            group.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromBody] ProjectMemberCreateDto dto,
                [FromServices] IProjectMemberWriteService projectMemberWriteSvc,
                CancellationToken ct = default) =>
            {
                var (result, _) = await projectMemberWriteSvc.CreateAsync(projectId, dto.UserId, dto.Role, dto.JoinedAt, ct);
                return result.ToHttp();
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireValidation<ProjectMemberCreateDto>()
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Add new project member")
            .WithDescription("Adds a user to the project as a member.")
            .WithName("ProjectMembers_Create");

            // PATCH /projects/{projectId}/members/{userId}/role
            group.MapPatch("/{userId:guid}/role", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromBody] ProjectMemberChangeRoleDto dto,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                [FromServices] IProjectMemberWriteService projectMemberWriteSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    http, () => projectMemberReadSvc.GetAsync(projectId, userId, ct), pm => pm.RowVersion);

                var result = await projectMemberWriteSvc.ChangeRoleAsync(projectId, userId, dto.NewRole, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(http);

                var updated = await projectMemberReadSvc.GetAsync(projectId, userId, ct);
                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(updated!.RowVersion)}\"";
                return Results.Ok(updated.ToReadDto());
            })
            .RequireAuthorization(Policies.ProjectOwner)
            .RequireValidation<ProjectMemberChangeRoleDto>()
            .RequireIfMatch()
            .Produces<ProjectMemberReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Change role to a project member")
            .WithDescription("Changes the role of a project member.")
            .WithName("ProjectMembers_Change_Role");

            // PATCH /projects/{projectId}/members/{userId}/remove
            group.MapPatch("/{userId:guid}/remove", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromBody] ProjectMemberRemoveDto dto,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                [FromServices] IProjectMemberWriteService projectMemberWriteSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    http, () => projectMemberReadSvc.GetAsync(projectId, userId, ct), pm => pm.RowVersion);

                var result = await projectMemberWriteSvc.RemoveAsync(projectId, userId, rowVersion, dto.RemovedAt, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(http);

                var removed = await projectMemberReadSvc.GetAsync(projectId, userId, ct);
                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(removed!.RowVersion)}\"";
                return Results.Ok(removed.ToReadDto());
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireValidation<ProjectMemberRemoveDto>()
            .RequireIfMatch()
            .Produces<ProjectMemberReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Remove project member")
            .WithDescription("Soft-removes a project member.")
            .WithName("ProjectMembers_Remove");

            // PATCH /projects/{projectId}/members/{userId}/restore
            group.MapPatch("/{userId:guid}/restore", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                [FromServices] IProjectMemberWriteService projectMemberWriteSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    http, () => projectMemberReadSvc.GetAsync(projectId, userId, ct), pm => pm.RowVersion);

                var result = await projectMemberWriteSvc.RestoreAsync(projectId, userId, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(http);

                var removed = await projectMemberReadSvc.GetAsync(projectId, userId, ct);
                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(removed!.RowVersion)}\"";
                return Results.Ok(removed.ToReadDto());
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireIfMatch()
            .Produces<ProjectMemberReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Restore project member")
            .WithDescription("Restores a previously removed project member.")
            .WithName("ProjectMembers_Restore");

            var top = app.MapGroup("/members")
                .WithTags("Project Members")
                .RequireAuthorization();

            // GET /members/{userId}/count
            top.MapGet("/{userId:guid}/count", async (
                [FromRoute] Guid userId,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var count = await projectMemberReadSvc.CountActiveAsync(userId, ct);
                return Results.Ok(new { Count = count });
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .Produces<object>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get total active project memberships of a user")
            .WithDescription("Returns the total number of active projects in which the specified user is a member.")
            .WithName("ProjectMembers_CountActiveByUser");

            return top;
        }
    }
}
