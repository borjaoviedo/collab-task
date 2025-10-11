using Api.Auth.Authorization;
using Api.Extensions;
using Application.ProjectMembers.Abstractions;
using Application.ProjectMembers.DTOs;
using Application.ProjectMembers.Mapping;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    public static class ProjectMembersEndpoints
    {
        public sealed record AddMemberDto(Guid UserId, ProjectRole Role, DateTimeOffset JoinedAtUtc);
        public sealed record ChangeMemberRoleDto(ProjectRole Role, byte[] RowVersion);
        public sealed record RemoveMemberDto(byte[] RowVersion, DateTimeOffset RemovedAtUtc);
        public sealed record RestoreMemberDto(byte[] RowVersion);

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
            .WithDescription("Requires reader or higher role in the target project.")
            .WithName("Project_Members_Get_All");

            // POST /projects/{projectId}/members
            group.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromBody] AddMemberDto dto,
                [FromServices] IProjectMemberWriteService svc,
                CancellationToken ct = default) =>
            {
                var (res, _) = await svc.CreateAsync(projectId, dto.UserId, dto.Role, dto.JoinedAtUtc.ToUniversalTime(), ct);

                if (res != DomainMutation.Created) return res.ToHttp();

                return res.ToHttp(location: $"/projects/{projectId}/members/{dto.UserId}");
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Add new project member")
            .WithDescription("Requires admin role in the target project")
            .WithName("Project_Members_Create");

            // PATCH /projects/{projectId}/members/{userId}/role
            group.MapPatch("/{userId:guid}/role", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromBody] ChangeMemberRoleDto dto,
                [FromServices] IProjectMemberWriteService svc,
                CancellationToken ct = default) =>
            {
                var res = await svc.ChangeRoleAsync(projectId, userId, dto.Role, dto.RowVersion, ct);
                return res.ToHttp();
            })
            .RequireAuthorization(Policies.ProjectOwner)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Change role to a project member")
            .WithDescription("Requires owner role in the target project")
            .WithName("Project_Members_Change_Role");

            // PATCH /projects/{projectId}/members/{userId}/remove
            group.MapPatch("/{userId:guid}/remove", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromBody] RemoveMemberDto dto,
                [FromServices] IProjectMemberWriteService svc,
                CancellationToken ct = default) =>
            {
                var res = await svc.RemoveAsync(projectId, userId, dto.RowVersion, dto.RemovedAtUtc.ToUniversalTime(), ct);
                return res.ToHttp();
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Remove project member")
            .WithDescription("Requires admin role in the target project")
            .WithName("Project_Members_Remove");

            // PATCH /projects/{projectId}/members/{userId}/restore
            group.MapPatch("/{userId:guid}/restore", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromBody] RestoreMemberDto dto,
                [FromServices] IProjectMemberWriteService svc,
                CancellationToken ct = default) =>
            {
                var res = await svc.RestoreAsync(projectId, userId, dto.RowVersion, ct);
                return res.ToHttp();
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Restore project member")
            .WithDescription("Requires admin role in the target project")
            .WithName("Project_Members_Restore");

            return group;
        }
    }
}
