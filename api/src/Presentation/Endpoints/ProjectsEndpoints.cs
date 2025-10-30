using Api.Auth.Authorization;
using Api.Extensions;
using Api.Helpers;
using Application.Common.Abstractions.Auth;
using Application.Projects.Abstractions;
using Application.Projects.DTOs;
using Application.Projects.Filters;
using Application.Projects.Mapping;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    /// <summary>
    /// Project endpoints: list, read, create, rename, and delete.
    /// Uses Clean Architecture services, per-endpoint auth, and optimistic concurrency via ETag/If-Match.
    /// </summary>
    public static class ProjectsEndpoints
    {
        /// <summary>
        /// Registers project endpoints under /projects and wires handlers, validation, and metadata.
        /// </summary>
        /// <param name="app">Endpoint route builder.</param>
        /// <returns>The configured route group.</returns>
        public static RouteGroupBuilder MapProjects(this IEndpointRouteBuilder app)
        {
            // Group all project endpoints; default requires authentication
            // Specific routes tighten auth further
            var group = app
                        .MapGroup("/projects")
                        .WithTags("Projects")
                        .RequireAuthorization();

            // OpenAPI metadata across all endpoints: ensures generated clients and API docs
            // include consistent success/error shapes, auth requirements, and concurrency responses

            // GET /projects?filter=...
            group.MapGet("/", async (
                [AsParameters] ProjectFilter filter,
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] IProjectReadService projectReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Projects.Get_All");

                // Caller-scoped listing. Uses current user's id and an optional filter bound via [AsParameters]
                var userId = (Guid)currentUserSvc.UserId!;
                var projects = await projectReadSvc.ListByUserAsync(userId, filter, ct);
                var responseDto = projects.Select(p => p.ToReadDto(userId)).ToList();

                log.LogInformation(
                    "Projects listed userId={UserId} filter={Filter} count={Count}",
                    userId,
                    filter,
                    responseDto.Count);
                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<ProjectReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("List accessible projects")
            .WithDescription("Returns projects where the caller has at least reader rights. Respects optional filters.")
            .WithName("Projects_Get_All");

            // GET /projects/{projectId}
            group.MapGet("/{projectId:guid}", async (
                [FromRoute] Guid projectId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] IProjectRepository projectRepository,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Projects.Get_ById");

                // Direct repository read for a single aggregate. Return 404 when absent
                var project = await projectRepository.GetByIdAsync(projectId, ct);
                if (project is null)
                {
                    log.LogInformation("Project not found projectId={ProjectId}", projectId);
                    return Results.NotFound();
                }

                // Compute weak ETag from RowVersion so clients can cache and perform conditional requests
                var userId = (Guid)currentUserSvc.UserId!;
                var responseDto = project.ToReadDto(userId);
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Project fetched projectId={ProjectId} etag={ETag}",
                    projectId,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectReader) // Require at least project-level Reader to fetch a single project
            .Produces<ProjectReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get project")
            .WithDescription("Returns the project if the caller has reader rights. Sets ETag.")
            .WithName("Projects_Get_ById");

            // GET /projects/me
            group.MapGet("/me", async (
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] IProjectReadService projectReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Projects.Get_Mine");

                // List projects for the authenticated user without extra filters
                var userId = (Guid)currentUserSvc.UserId!;
                var projects = await projectReadSvc.ListByUserAsync(userId, filter: null, ct);
                var responseDto = projects.Select(p => p.ToReadDto(userId)).ToList();

                log.LogInformation(
                    "Projects listed for current user userId={UserId} count={Count}",
                    userId,
                    responseDto.Count);
                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<ProjectReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("List my projects")
            .WithDescription("Returns projects accessible to the authenticated user.")
            .WithName("Projects_Get_Mine");

            // GET /projects/users/{userId}
            group.MapGet("/users/{userId:guid}", async (
                [FromRoute] Guid userId,
                [FromServices] ILoggerFactory logger,
                [FromServices] IProjectReadService projectReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Projects.Get_ByUser");

                // Admin view of another user's accessible projects
                var projects = await projectReadSvc.ListByUserAsync(userId, filter: null, ct);
                var responseDto = projects.Select(p => p.ToReadDto(userId)).ToList();

                log.LogInformation(
                    "Projects listed for userId={UserId} count={Count}",
                    userId,
                    responseDto.Count);
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.SystemAdmin) // SystemAdmin-only
            .Produces<IEnumerable<ProjectReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List projects by user")
            .WithDescription("Admin-only. Returns projects accessible to the specified user.")
            .WithName("Projects_Get_ByUser");

            // POST /projects
            group.MapPost("/", async (
                [FromBody] ProjectCreateDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] IProjectWriteService projectWriteSvc,
                [FromServices] IProjectReadService projectReadSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Projects.Create");

                // Owner-initiated create. Name is a domain VO; owner is the current user
                var userId = (Guid)currentUserSvc.UserId!;
                var projectName = ProjectName.Create(dto.Name);

                var (result, project) = await projectWriteSvc.CreateAsync(userId, projectName, ct);

                if (result != DomainMutation.Created || project is null)
                {
                    log.LogInformation(
                        "Project create rejected userId={UserId} mutation={Mutation}",
                        userId,
                        result);
                    return result.ToHttp(context);
                }
                // Map DomainMutation to HTTP. On success, return canonical representation and ETag
                var responseDto = project.ToReadDto(userId);
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Project created projectId={ProjectId} ownerId={UserId} etag={ETag}",
                    project.Id,
                    userId,
                    etag);

                // Location header is set via route name for stable client navigation
                var routeValues = new { projectId = project.Id };
                return Results.CreatedAtRoute("Projects_Get_ById", routeValues, responseDto).WithETag(etag);
            })
            .RequireValidation<ProjectCreateDto>()
            .RejectIfMatch() // Reject If-Match on create: preconditions do not apply to new resources
            .Produces<ProjectReadDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Create project")
            .WithDescription("Creates a project owned by the caller. Returns the resource with ETag.")
            .WithName("Projects_Create");

            // PATCH /projects/{projectId}/rename
            group.MapPatch("/{projectId:guid}/rename", async (
                [FromRoute] Guid projectId,
                [FromBody] ProjectRenameDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] IProjectReadService projectReadSvc,
                [FromServices] IProjectWriteService projectWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Projects.Rename");

                // Resolve current RowVersion from If-Match or storage fallback to guard against lost updates
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => projectReadSvc.GetAsync(projectId, ct),
                    p => p.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "Project not found when resolving row version projectId={ProjectId}",
                        projectId);
                    return Results.NotFound();
                }

                // Rename under optimistic concurrency. Return 412 on stale precondition
                var projectName = ProjectName.Create(dto.NewName);
                var result = await projectWriteSvc.RenameAsync(projectId, projectName, rowVersion, ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation(
                        "Project rename rejected projectId={ProjectId} mutation={Mutation}",
                        projectId,
                        result);
                    return result.ToHttp(context);
                }

                // Read back to return fresh state and a new ETag
                var edited = await projectReadSvc.GetAsync(projectId, ct);
                if (edited is null)
                {
                    log.LogInformation(
                        "Project rename readback missing projectId={ProjectId}",
                        projectId);
                    return Results.NotFound();
                }

                var userId = (Guid)currentUserSvc.UserId!;
                var responseDto = edited.ToReadDto(userId);
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Project renamed projectId={ProjectId} newName={NewName} etag={ETag}",
                    projectId,
                    dto.NewName,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireValidation<ProjectRenameDto>()
            .RequireIfMatch() // Require If-Match to prevent overwriting concurrent edits
            .Produces<ProjectReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Rename project")
            .WithDescription("Updates the project name using optimistic concurrency (If-Match). Returns the updated resource and ETag.")
            .WithName("Projects_Rename");

            // DELETE /projects/{projectId}
            group.MapDelete("/{projectId:guid}", async (
                [FromRoute] Guid projectId,
                [FromServices] ILoggerFactory logger,
                [FromServices] IProjectReadService projectReadSvc,
                [FromServices] IProjectWriteService projectWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Projects.Delete");

                // Conditional delete. Resolve RowVersion and map DomainMutation to HTTP (204, 404, 409, 412)
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => projectReadSvc.GetAsync(projectId, ct),
                    p => p.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation("Project not found when resolving row version projectId={ProjectId}", projectId);
                    return Results.NotFound();
                }

                var result = await projectWriteSvc.DeleteAsync(projectId, rowVersion, ct);

                log.LogInformation(
                    "Project delete result projectId={ProjectId} mutation={Mutation}",
                    projectId,
                    result);
                return result.ToHttp(context);
            })
            .RequireAuthorization(Policies.ProjectOwner) // ProjectOwner-only
            .RequireIfMatch() // Requires If-Match to avoid deleting over stale state
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Delete project")
            .WithDescription("Owner-only. Deletes the project using optimistic concurrency (If-Match).")
            .WithName("Projects_Delete");

            return group;
        }
    }
}
