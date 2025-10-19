using Api.Auth.Authorization;
using Api.Extensions;
using Api.Helpers;
using Application.Common.Abstractions.Auth;
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
            var nested = app.MapGroup("/projects/{projectId:guid}/members")
                .WithTags("Project Members")
                .RequireAuthorization(Policies.ProjectReader);

            // GET /projects/{projectId}/members
            nested.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromQuery] bool includeRemoved,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var members = await projectMemberReadSvc.ListByProjectAsync(projectId, includeRemoved, ct);
                var responseDto = members.Select(m => m.ToReadDto()).ToList();

                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<ProjectMemberReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("Get all members of a project")
            .WithDescription("Returns all members of the project.")
            .WithName("ProjectMembers_Get_All");

            // GET /projects/{projectId}/members/{userId}
            nested.MapGet("/{userId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var member = await projectMemberReadSvc.GetAsync(projectId, userId, ct);
                if (member is null) return Results.NotFound();

                var responseDto = member.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                return Results.Ok(responseDto);
            })
            .Produces<ProjectMemberReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get project member")
            .WithDescription("Returns a project member by project and user id.")
            .WithName("ProjectMembers_Get_ById");

            // GET /projects/{projectId}/members/{userId}/role
            nested.MapGet("/{userId:guid}/role", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var role = await projectMemberReadSvc.GetRoleAsync(projectId, userId, ct);
                if (role is null) return Results.NotFound();

                var responseDto = role.Value.ToRoleReadDto();

                return Results.Ok(responseDto);
            })
            .Produces<ProjectMemberRoleReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get member role")
            .WithDescription("Returns the role of a user within a project.")
            .WithName("ProjectMembers_Get_Role");

            // POST /projects/{projectId}/members
            nested.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromBody] ProjectMemberCreateDto dto,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                [FromServices] IProjectMemberWriteService projectMemberWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var (result, member) = await projectMemberWriteSvc.CreateAsync(projectId, dto.UserId, dto.Role, ct);
                if (result != DomainMutation.Created || member is null) return result.ToHttp();

                var created = await projectMemberReadSvc.GetAsync(projectId, member.UserId, ct);
                if (created is null) return Results.NotFound();

                var responseDto = created.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                return Results.CreatedAtRoute(
                    "ProjectMembers_Get_ById",
                    new { projectId, userId = dto.UserId },
                    responseDto);
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireValidation<ProjectMemberCreateDto>()
            .Produces<ProjectMemberReadDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Add new project member")
            .WithDescription("Adds a user to the project as a member.")
            .WithName("ProjectMembers_Create");

            // PATCH /projects/{projectId}/members/{userId}/role
            nested.MapPatch("/{userId:guid}/role", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromBody] ProjectMemberChangeRoleDto dto,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                [FromServices] IProjectMemberWriteService projectMemberWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => projectMemberReadSvc.GetAsync(projectId, userId, ct), pm => pm.RowVersion);

                var result = await projectMemberWriteSvc.ChangeRoleAsync(projectId, userId, dto.NewRole, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(context);

                var updated = await projectMemberReadSvc.GetAsync(projectId, userId, ct);
                if (updated is null) return Results.NotFound();

                var responseDto = updated.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireValidation<ProjectMemberChangeRoleDto>()
            .RequireIfMatch()
            .Produces<ProjectMemberReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Change role to a project member")
            .WithDescription("Changes the role of a project member.")
            .WithName("ProjectMembers_ChangeRole");

            // PATCH /projects/{projectId}/members/{userId}/remove
            nested.MapPatch("/{userId:guid}/remove", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                [FromServices] IProjectMemberWriteService projectMemberWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => projectMemberReadSvc.GetAsync(projectId, userId, ct), pm => pm.RowVersion);

                var result = await projectMemberWriteSvc.RemoveAsync(projectId, userId, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(context);

                var removed = await projectMemberReadSvc.GetAsync(projectId, userId, ct);
                if (removed is null) return Results.NotFound();

                var responseDto = removed.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireIfMatch()
            .Produces<ProjectMemberReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Remove project member")
            .WithDescription("Soft-removes a project member.")
            .WithName("ProjectMembers_Remove");

            // PATCH /projects/{projectId}/members/{userId}/restore
            nested.MapPatch("/{userId:guid}/restore", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                [FromServices] IProjectMemberWriteService projectMemberWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => projectMemberReadSvc.GetAsync(projectId, userId, ct), pm => pm.RowVersion);

                var result = await projectMemberWriteSvc.RestoreAsync(projectId, userId, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(context);

                var removed = await projectMemberReadSvc.GetAsync(projectId, userId, ct);
                if (removed is null) return Results.NotFound();

                var responseDto = removed.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireIfMatch()
            .Produces<ProjectMemberReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Restore project member")
            .WithDescription("Restores a previously removed project member.")
            .WithName("ProjectMembers_Restore");

            var top = app.MapGroup("/members")
                .WithTags("Project Members")
                .RequireAuthorization();

            // GET /members/me/count
            top.MapGet("/me/count", async (
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var userId = (Guid)currentUserSvc.UserId!;
                var count = await projectMemberReadSvc.CountActiveAsync(userId, ct);
                var responseDto = count.ToCountReadDto();

                return Results.Ok(responseDto);
            })
            .Produces<ProjectMemberCountReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("Get total active project memberships of the authenticated user")
            .WithDescription("Returns the total number of active projects in which the authenticated user is a member.")
            .WithName("ProjectMembers_CountActive_Mine");

            // GET /members/{userId}/count
            top.MapGet("/{userId:guid}/count", async (
                [FromRoute] Guid userId,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var count = await projectMemberReadSvc.CountActiveAsync(userId, ct);
                var responseDto = count.ToCountReadDto();

                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.SystemAdmin)
            .Produces<ProjectMemberCountReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("Get total active project memberships of the specified user")
            .WithDescription("Returns the total number of active projects in which the specified user is a member.")
            .WithName("ProjectMembers_CountActive_ByUser");

            return top;
        }
    }
}
