using Api.Auth.Authorization;
using Api.Concurrency;
using Api.Filters;
using Application.Projects.Abstractions;
using Application.Projects.DTOs;
using Application.Projects.Filters;
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
                [FromServices] IProjectReadService projectReadSvc,
                CancellationToken ct = default) =>
            {
                var projectReadDtoList = await projectReadSvc.ListSelfAsync(filter, ct);
                return Results.Ok(projectReadDtoList);
            })
            .Produces<IEnumerable<ProjectReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("List accessible projects")
            .WithDescription("Returns projects where the caller has at least reader rights. Respects optional filters.")
            .WithName("Projects_Get_All");

            // GET /projects/{projectId}
            group.MapGet("/{projectId:guid}", async (
                [FromRoute] Guid projectId,
                [FromServices] IProjectReadService projectReadSvc,
                CancellationToken ct = default) =>
            {
                var projectReadDto = await projectReadSvc.GetByIdAsync(projectId, ct);
                var etag = ETag.EncodeWeak(projectReadDto.RowVersion);

                return Results.Ok(projectReadDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectReader) // Require at least project-level Reader to fetch a single project
            .Produces<ProjectReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get project")
            .WithDescription("Returns the project if the caller has reader rights. Sets ETag.")
            .WithName("Projects_Get_ById");

            // GET /projects/users/{userId}
            group.MapGet("/users/{userId:guid}", async (
                [FromRoute] Guid userId,
                [FromServices] IProjectReadService projectReadSvc,
                CancellationToken ct = default) =>
            {
                var projectReadDtoList = await projectReadSvc.ListByUserIdAsync(userId, filter: null, ct);
                return Results.Ok(projectReadDtoList);
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
                [FromServices] IProjectWriteService projectWriteSvc,
                CancellationToken ct = default) =>
            {
                var projectReadDto = await projectWriteSvc.CreateAsync(dto, ct);
                var etag = ETag.EncodeWeak(projectReadDto.RowVersion);

                // Location header is set via route name for stable client navigation
                var routeValues = new { projectId = projectReadDto.Id };
                return Results.CreatedAtRoute("Projects_Get_ById", routeValues, projectReadDto).WithETag(etag);
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
                [FromServices] IProjectWriteService projectWriteSvc,
                CancellationToken ct = default) =>
            {
                var projectReadDto = await projectWriteSvc.RenameAsync(projectId, dto, ct);
                var etag = ETag.EncodeWeak(projectReadDto.RowVersion);

                return Results.Ok(projectReadDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireValidation<ProjectRenameDto>()
            .RequireIfMatch() // Require If-Match to prevent overwriting concurrent edits
            .EnsureIfMatch<IProjectReadService, ProjectReadDto>(routeValueKey: "projectId")
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
                [FromServices] IProjectWriteService projectWriteSvc,
                CancellationToken ct = default) =>
            {
                await projectWriteSvc.DeleteByIdAsync(projectId, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Policies.ProjectOwner) // ProjectOwner-only
            .RequireIfMatch() // Requires If-Match to avoid deleting over stale state
            .EnsureIfMatch<IProjectReadService, ProjectReadDto>(routeValueKey: "projectId")
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
