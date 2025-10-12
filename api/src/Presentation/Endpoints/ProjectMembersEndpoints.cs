using Api.Auth.Authorization;
using Api.Extensions;
using Api.Filters;
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
                .RequireAuthorization();

            // GET /projects/{projectId}/members
            group.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromQuery] bool includeRemoved,
                [FromServices] IProjectMemberReadService svc,
                CancellationToken ct = default) =>
            {
                var members = await svc.ListByProjectAsync(projectId, includeRemoved, ct);
                var dto = members.Select(m => m.ToReadDto()).ToList();
                return Results.Ok(dto);
            })
            .RequireAuthorization(Policies.ProjectReader)
            .Produces<IEnumerable<ProjectMemberReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get all members of a project")
            .WithDescription("Returns all members of the project.")
            .WithName("Project_Members_Get_All");

            // POST /projects/{projectId}/members
            group.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromBody] ProjectMemberCreateDto dto,
                [FromServices] IProjectMemberWriteService svc,
                CancellationToken ct = default) =>
            {
                var (result, _) = await svc.CreateAsync(projectId, dto.UserId, dto.Role, dto.JoinedAt, ct);
                return result.ToHttp();
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Add new project member")
            .WithDescription("Adds a user to the project as a member.")
            .WithName("Project_Members_Create");

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
                var rowVersion = (byte[])http.Items["rowVersion"]!;
                var result = await projectMemberWriteSvc.ChangeRoleAsync(projectId, userId, dto.NewRole, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp();

                var updated = await projectMemberReadSvc.GetAsync(projectId, userId, ct);
                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(updated!.RowVersion)}\"";
                return Results.Ok(updated.ToReadDto());
            })
            .AddEndpointFilter<IfMatchRowVersionFilter>()
            .RequireAuthorization(Policies.ProjectOwner)
            .Produces<ProjectMemberReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Change role to a project member")
            .WithDescription("Changes the role of a project member.")
            .WithName("Project_Members_Change_Role");

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
                var rowVersion = (byte[])http.Items["rowVersion"]!;
                var result = await projectMemberWriteSvc.RemoveAsync(projectId, userId, rowVersion, dto.RemovedAt, ct);
                if (result != DomainMutation.Updated) return result.ToHttp();

                var removed = await projectMemberReadSvc.GetAsync(projectId, userId, ct);
                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(removed!.RowVersion)}\"";
                return Results.Ok(removed.ToReadDto());
            })
            .AddEndpointFilter<IfMatchRowVersionFilter>()
            .RequireAuthorization(Policies.ProjectAdmin)
            .Produces<ProjectMemberReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Remove project member")
            .WithDescription("Soft-removes a project member.")
            .WithName("Project_Members_Remove");

            // PATCH /projects/{projectId}/members/{userId}/restore
            group.MapPatch("/{userId:guid}/restore", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                [FromServices] IProjectMemberWriteService projectMemberWriteSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = (byte[])http.Items["rowVersion"]!;
                var result = await projectMemberWriteSvc.RestoreAsync(projectId, userId, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp();

                var removed = await projectMemberReadSvc.GetAsync(projectId, userId, ct);
                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(removed!.RowVersion)}\"";
                return Results.Ok(removed.ToReadDto());
            })
            .AddEndpointFilter<IfMatchRowVersionFilter>()
            .RequireAuthorization(Policies.ProjectAdmin)
            .Produces<ProjectMemberReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Restore project member")
            .WithDescription("Restores a previously removed project member.")
            .WithName("Project_Members_Restore");

            return group;
        }
    }
}
