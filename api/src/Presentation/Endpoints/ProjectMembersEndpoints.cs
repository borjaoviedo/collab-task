using Api.Auth.Authorization;
using Api.Concurrency;
using Api.Extensions;
using Application.Common.Abstractions.Auth;
using Application.ProjectMembers.Abstractions;
using Application.ProjectMembers.DTOs;
using Application.ProjectMembers.Mapping;
using Domain.Enums;
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
        /// Registers project member endpoints under both project-scoped and global routes.
        /// Enforces auth at group level and applies validation and concurrency semantics per endpoint.
        /// </summary>
        /// <param name="app">Endpoint route builder.</param>
        /// <returns>The configured route group.</returns>
        public static RouteGroupBuilder MapProjectMembers(this IEndpointRouteBuilder app)
        {
            // Group all project-scoped member endpoints; minimum access is ProjectReader
            var nested = app
                .MapGroup("/projects/{projectId:guid}/members")
                .WithTags("Project Members")
                .RequireAuthorization(Policies.ProjectReader);

            // OpenAPI metadata across all endpoints: ensures generated clients and API docs
            // include consistent success/error shapes, auth requirements, and concurrency responses

            // GET /projects/{projectId}/members
            nested.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromQuery] bool includeRemoved,
                [FromServices] ILoggerFactory logger,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("ProjectMembers.Get_All");

                // Read-side list with optional inclusion of soft-removed members for audit/admin views
                var members = await projectMemberReadSvc.ListByProjectAsync(projectId, includeRemoved, ct);
                var responseDto = members.Select(m => m.ToReadDto()).ToList();

                log.LogInformation(
                    "Project members listed projectId={ProjectId} includeRemoved={IncludeRemoved} count={Count}",
                    projectId,
                    includeRemoved,
                    responseDto.Count);
                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<ProjectMemberReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List project members")
            .WithDescription("Returns project members. Can include removed members.")
            .WithName("ProjectMembers_Get_All");

            // GET /projects/{projectId}/members/{userId}
            nested.MapGet("/{userId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromServices] ILoggerFactory logger,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("ProjectMembers.Get_ById");

                // Fetch a specific membership entry. Return 404 when not present in the project scope
                var member = await projectMemberReadSvc.GetAsync(projectId, userId, ct);
                if (member is null)
                {
                    log.LogInformation(
                        "Project member not found projectId={ProjectId} userId={UserId}",
                        projectId,
                        userId);
                    return Results.NotFound();
                }

                // ETag from RowVersion enables conditional reads and optimistic concurrency
                var responseDto = member.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Project member fetched projectId={ProjectId} userId={UserId} etag={ETag}",
                    projectId,
                    userId,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
            })
            .Produces<ProjectMemberReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get project member")
            .WithDescription("Returns a member entry. Sets ETag.")
            .WithName("ProjectMembers_Get_ById");

            // GET /projects/{projectId}/members/{userId}/role
            nested.MapGet("/{userId:guid}/role", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromServices] ILoggerFactory logger,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("ProjectMembers.Get_Role");

                // Lightweight role read for authorization-aware UIs; avoids fetching full membership
                var role = await projectMemberReadSvc.GetRoleAsync(projectId, userId, ct);
                if (role is null)
                {
                    log.LogInformation(
                        "Project member role not found projectId={ProjectId} userId={UserId}",
                        projectId,
                        userId);
                    return Results.NotFound();
                }

                var responseDto = role.Value.ToRoleReadDto();

                log.LogInformation(
                    "Project member role fetched projectId={ProjectId} userId={UserId} role={Role}",
                    projectId,
                    userId,
                    role);
                return Results.Ok(responseDto);
            })
            .Produces<ProjectMemberRoleReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get member role")
            .WithDescription("Returns the role of the user in the project.")
            .WithName("ProjectMembers_Get_Role");

            // POST /projects/{projectId}/members
            nested.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromBody] ProjectMemberCreateDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                [FromServices] IProjectMemberWriteService projectMemberWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("ProjectMembers.Create");

                // Write side: add user to project. DomainMutation drives HTTP mapping
                var (result, member) = await projectMemberWriteSvc.CreateAsync(projectId, dto.UserId, dto.Role, ct);
                if (result != DomainMutation.Created || member is null)
                {
                    log.LogInformation(
                        "Project member create rejected projectId={ProjectId} userId={UserId} mutation={Mutation}",
                        projectId,
                        dto.UserId,
                        result);
                    return result.ToHttp(context);
                }

                // Read back to return canonical representation and attach ETag
                var created = await projectMemberReadSvc.GetAsync(projectId, member.UserId, ct);
                if (created is null)
                {
                    log.LogInformation(
                        "Project member create readback missing projectId={ProjectId} userId={UserId}",
                        projectId,
                        dto.UserId);
                    return Results.NotFound();
                }

                var responseDto = created.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Project member created projectId={ProjectId} userId={UserId} role={Role} etag={ETag}",
                    projectId,
                    dto.UserId,
                    created.Role,
                    etag);

                // Location header uses route name for stable navigation
                var routeValues = new { projectId, userId = dto.UserId };
                return Results.CreatedAtRoute("ProjectMembers_Get_ById", routeValues, responseDto).WithETag(etag);
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
            nested.MapPatch("/{userId:guid}/role", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromBody] ProjectMemberChangeRoleDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                [FromServices] IProjectMemberWriteService projectMemberWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("ProjectMembers.ChangeRole");

                // Resolve RowVersion from If-Match or storage to guard against lost updates
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => projectMemberReadSvc.GetAsync(projectId, userId, ct),
                    pm => pm.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "Project member not found when resolving row version projectId={ProjectId} userId={UserId}",
                        projectId,
                        userId);
                    return Results.NotFound();
                }

                // Apply role change under optimistic concurrency. Map stale to 412 Precondition Failed
                var result = await projectMemberWriteSvc.ChangeRoleAsync(projectId, userId, dto.NewRole, rowVersion, ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation(
                        "Project member role change rejected projectId={ProjectId} userId={UserId} mutation={Mutation}",
                        projectId,
                        userId,
                        result);
                    return result.ToHttp(context);
                }

                // Return fresh representation with updated ETag
                var updated = await projectMemberReadSvc.GetAsync(projectId, userId, ct);
                if (updated is null)
                {
                    log.LogInformation(
                        "Project member role change readback missing projectId={ProjectId} userId={UserId}",
                        projectId,
                        userId);
                    return Results.NotFound();
                }

                var responseDto = updated.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Project member role changed projectId={ProjectId} userId={UserId} newRole={NewRole} etag={ETag}",
                    projectId,
                    userId,
                    dto.NewRole,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
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
            nested.MapPatch("/{userId:guid}/remove", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromServices] ILoggerFactory logger,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                [FromServices] IProjectMemberWriteService projectMemberWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("ProjectMembers.Remove");

                // Soft-remove membership under optimistic concurrency
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => projectMemberReadSvc.GetAsync(projectId, userId, ct),
                    pm => pm.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "Project member not found when resolving row version projectId={ProjectId} userId={UserId}",
                        projectId,
                        userId);
                    return Results.NotFound();
                }

                var result = await projectMemberWriteSvc.RemoveAsync(projectId, userId, rowVersion, ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation(
                        "Project member remove rejected projectId={ProjectId} userId={UserId} mutation={Mutation}",
                        projectId,
                        userId,
                        result);
                    return result.ToHttp(context);
                }

                // Return updated state and a refreshed ETag so clients can proceed with further mutations
                var removed = await projectMemberReadSvc.GetAsync(projectId, userId, ct);
                if (removed is null)
                {
                    log.LogInformation(
                        "Project member remove readback missing projectId={ProjectId} userId={UserId}",
                        projectId,
                        userId);
                    return Results.NotFound();
                }

                var responseDto = removed.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Project member removed projectId={ProjectId} userId={UserId} etag={ETag}",
                    projectId,
                    userId,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
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
            nested.MapPatch("/{userId:guid}/restore", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromServices] ILoggerFactory logger,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                [FromServices] IProjectMemberWriteService projectMemberWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("ProjectMembers.Restore");

                // Restore a previously soft-removed membership using optimistic concurrency
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => projectMemberReadSvc.GetAsync(projectId, userId, ct),
                    pm => pm.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "Project member not found when resolving row version projectId={ProjectId} userId={UserId}",
                        projectId,
                        userId);
                    return Results.NotFound();
                }

                var result = await projectMemberWriteSvc.RestoreAsync(projectId, userId, rowVersion, ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation(
                        "Project member restore rejected projectId={ProjectId} userId={UserId} mutation={Mutation}",
                        projectId,
                        userId,
                        result);
                    return result.ToHttp(context);
                }

                // Return updated state and ETag for subsequent operations
                var removed = await projectMemberReadSvc.GetAsync(projectId, userId, ct);
                if (removed is null)
                {
                    log.LogInformation(
                        "Project member restore readback missing projectId={ProjectId} userId={UserId}",
                        projectId,
                        userId);
                    return Results.NotFound();
                }

                var responseDto = removed.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Project member restored projectId={ProjectId} userId={UserId} etag={ETag}",
                    projectId,
                    userId,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
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
            var top = app.MapGroup("/members")
                .WithTags("Project Members")
                .RequireAuthorization();

            // GET /members/me/count
            top.MapGet("/me/count", async (
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("ProjectMembers.CountActive_Mine");

                // Count active project memberships for the current authenticated user
                var userId = (Guid)currentUserSvc.UserId!;
                var count = await projectMemberReadSvc.CountActiveAsync(userId, ct);
                var responseDto = count.ToCountReadDto();

                log.LogInformation(
                    "Project membership count fetched for current user userId={UserId} count={Count}",
                    userId,
                    count);
                return Results.Ok(responseDto);
            })
            .Produces<ProjectMemberCountReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("Count my active memberships")
            .WithDescription("Returns the number of active projects for the authenticated user.")
            .WithName("ProjectMembers_CountActive_Mine");

            // GET /members/{userId}/count
            top.MapGet("/{userId:guid}/count", async (
                [FromRoute] Guid userId,
                [FromServices] ILoggerFactory logger,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("ProjectMembers.CountActive_ByUser");

                // Admin-only variant to count active memberships for an arbitrary user
                var count = await projectMemberReadSvc.CountActiveAsync(userId, ct);
                var responseDto = count.ToCountReadDto();

                log.LogInformation(
                    "Project membership count fetched for userId={UserId} count={Count}",
                    userId,
                    count);
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.SystemAdmin) // SystemAdmin-only
            .Produces<ProjectMemberCountReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("Count active memberships by user")
            .WithDescription("Admin-only. Returns the number of active projects for the specified user.")
            .WithName("ProjectMembers_CountActive_ByUser");

            return top;
        }
    }
}
