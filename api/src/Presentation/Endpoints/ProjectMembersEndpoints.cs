using Api.Auth.Authorization;
using Api.Concurrency;
using Api.Filters;
using Application.ProjectMembers.Abstractions;
using Application.ProjectMembers.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    /// <summary>
    /// Project membership endpoints: list, read, role lookup, add, change role,
    /// soft-remove, restore, and membership counts. Uses optimistic concurrency (ETag/If-Match).
    /// </summary>
    public static class ProjectMembersEndpoints
    {
        /// <summary>
        /// Registers project member endpoints under:
        /// - /projects/{projectId}/members (project-scoped membership management),
        /// - /members (global membership utilities).
        /// Enforces auth at group level and applies validation and concurrency semantics per endpoint.
        /// </summary>
        /// <param name="app">Endpoint route builder.</param>
        /// <returns>The configured route group.</returns>
        public static RouteGroupBuilder MapProjectMembers(this IEndpointRouteBuilder app)
        {
            // Group all project-scoped member endpoints; minimum access is ProjectReader
            var projectMembersGroup = app
                .MapGroup("/projects/{projectId:guid}/members")
                .WithTags("Project Members")
                .RequireAuthorization(Policies.ProjectReader);

            // OpenAPI metadata across all endpoints: ensures generated clients and API docs
            // include consistent success/error shapes, auth requirements, and concurrency responses

            // GET /projects/{projectId}/members
            projectMembersGroup.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromQuery] bool includeRemoved,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var projectMemberReadDtoList = await projectMemberReadSvc.ListByProjectIdAsync(
                    projectId,
                    includeRemoved,
                    ct);
                return Results.Ok(projectMemberReadDtoList);
            })
            .Produces<IEnumerable<ProjectMemberReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("List project members")
            .WithDescription("Returns project members. Can include removed members.")
            .WithName("ProjectMembers_Get_All");

            // GET /projects/{projectId}/members/{userId}
            projectMembersGroup.MapGet("/{userId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var projectMemberReadDto = await projectMemberReadSvc.GetByProjectAndUserIdAsync(
                    projectId,
                    userId,
                    ct);
                var etag = ETag.EncodeWeak(projectMemberReadDto.RowVersion);

                return Results.Ok(projectMemberReadDto).WithETag(etag);
            })
            .Produces<ProjectMemberReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get project member")
            .WithDescription("Returns a member entry. Sets ETag.")
            .WithName("ProjectMembers_Get_ById");

            // GET /projects/{projectId}/members/{userId}/role
            projectMembersGroup.MapGet("/{userId:guid}/role", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var projectMemberRoleReadDto = await projectMemberReadSvc.GetUserRoleAsync(
                    projectId,
                    userId,
                    ct);
                return Results.Ok(projectMemberRoleReadDto);
            })
            .Produces<ProjectMemberRoleReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get member role")
            .WithDescription("Returns the role of the user in the project.")
            .WithName("ProjectMembers_Get_Role");

            // POST /projects/{projectId}/members
            projectMembersGroup.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromBody] ProjectMemberCreateDto dto,
                [FromServices] IProjectMemberWriteService projectMemberWriteSvc,
                CancellationToken ct = default) =>
            {
                var projectMemberReadDto = await projectMemberWriteSvc.CreateAsync(projectId, dto, ct);
                var etag = ETag.EncodeWeak(projectMemberReadDto.RowVersion);

                var routeValues = new { projectId, userId = dto.UserId };
                return Results
                    .CreatedAtRoute("ProjectMembers_Get_ById", routeValues, projectMemberReadDto)
                    .WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireValidation<ProjectMemberCreateDto>()
            .RejectIfMatch() // Reject If-Match on create: new resources must not carry preconditions
            .Produces<ProjectMemberReadDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Add project member")
            .WithDescription("Admin-only. Adds a user to the project. Returns the resource with ETag.")
            .WithName("ProjectMembers_Create");

            // PATCH /projects/{projectId}/members/{userId}/role
            projectMembersGroup.MapPatch("/{userId:guid}/role", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromBody] ProjectMemberChangeRoleDto dto,
                [FromServices] IProjectMemberWriteService projectMemberWriteSvc,
                CancellationToken ct = default) =>
            {
                var projectMemberReadDto = await projectMemberWriteSvc.ChangeRoleAsync(
                    projectId,
                    userId,
                    dto,
                    ct);
                var etag = ETag.EncodeWeak(projectMemberReadDto.RowVersion);

                return Results.Ok(projectMemberReadDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireValidation<ProjectMemberChangeRoleDto>()
            .RequireIfMatch() // Require If-Match to prevent overwriting concurrent role changes
            .Produces<ProjectMemberReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Change member role")
            .WithDescription("Admin-only. Changes role using optimistic concurrency (If-Match). Returns the updated resource and ETag.")
            .WithName("ProjectMembers_ChangeRole");

            // PATCH /projects/{projectId}/members/{userId}/remove
            projectMembersGroup.MapPatch("/{userId:guid}/remove", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromServices] IProjectMemberWriteService projectMemberWriteSvc,
                CancellationToken ct = default) =>
            {
                var projectMemberReadDto = await projectMemberWriteSvc.RemoveAsync(projectId, userId, ct);
                var etag = ETag.EncodeWeak(projectMemberReadDto.RowVersion);

                return Results.Ok(projectMemberReadDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireIfMatch() // Require If-Match to avoid removing a membership updated by someone else
            .Produces<ProjectMemberReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Remove project member")
            .WithDescription("Admin-only. Soft-removes a member using optimistic concurrency (If-Match). Returns the updated resource and ETag.")
            .WithName("ProjectMembers_Remove");

            // PATCH /projects/{projectId}/members/{userId}/restore
            projectMembersGroup.MapPatch("/{userId:guid}/restore", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromServices] IProjectMemberWriteService projectMemberWriteSvc,
                CancellationToken ct = default) =>
            {
                var projectMemberReadDto = await projectMemberWriteSvc.RestoreAsync(projectId, userId, ct);
                var etag = ETag.EncodeWeak(projectMemberReadDto.RowVersion);

                return Results.Ok(projectMemberReadDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireIfMatch() // Require If-Match to ensure restore does not clobber concurrent edits
            .Produces<ProjectMemberReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Restore project member")
            .WithDescription("Admin-only. Restores a previously removed member using optimistic concurrency (If-Match). Returns the updated resource and ETag.")
            .WithName("ProjectMembers_Restore");


            // Global member utilities not bound to a specific project scope
            var globalMembersGroup = app.MapGroup("/members")
                .WithTags("Project Members")
                .RequireAuthorization();

            // GET /members/me/count
            globalMembersGroup.MapGet("/me/count", async (
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var projectMemberCountReadDto = await projectMemberReadSvc.CountActiveSelfAsync(ct);
                return Results.Ok(projectMemberCountReadDto);
            })
            .Produces<ProjectMemberCountReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("Count my active memberships")
            .WithDescription("Returns the number of active projects for the authenticated user.")
            .WithName("ProjectMembers_CountActive_Mine");

            // GET /members/{userId}/count
            globalMembersGroup.MapGet("/{userId:guid}/count", async (
                [FromRoute] Guid userId,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var projectMemberCountReadDto = await projectMemberReadSvc.CountActiveUsersAsync(userId, ct);
                return Results.Ok(projectMemberCountReadDto);
            })
            .RequireAuthorization(Policies.SystemAdmin) // SystemAdmin-only
            .Produces<ProjectMemberCountReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Count active memberships by user")
            .WithDescription("Admin-only. Returns the number of active projects for the specified user.")
            .WithName("ProjectMembers_CountActive_ByUser");

            return globalMembersGroup;
        }
    }
}
