using Api.Auth.Authorization;
using Api.Extensions;
using Api.Helpers;
using Application.Common.Abstractions.Auth;
using Application.Projects.Abstractions;
using Application.Projects.DTOs;
using Application.Projects.Mapping;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    public static class ProjectsEndpoints
    {
        public static RouteGroupBuilder MapProjects(this IEndpointRouteBuilder app)
        {
            var group = app
                        .MapGroup("/projects")
                        .WithTags("Projects")
                        .RequireAuthorization();

            // GET /projects?filter=...
            group.MapGet("/", async (
                [AsParameters] ProjectFilter filter,
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] IProjectReadService projectReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Projects.Get_All");

                var userId = (Guid)currentUserSvc.UserId!;
                var projects = await projectReadSvc.GetAllByUserAsync(userId, filter, ct);
                var responseDto = projects.Select(p => p.ToReadDto(userId)).ToList();

                log.LogInformation("Projects listed userId={UserId} filter={Filter} count={Count}",
                                    userId, filter, responseDto.Count);
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

                var project = await projectRepository.GetByIdAsync(projectId, ct);
                if (project is null)
                {
                    log.LogInformation("Project not found projectId={ProjectId}", projectId);
                    return Results.NotFound();
                }

                var userId = (Guid)currentUserSvc.UserId!;
                var responseDto = project.ToReadDto(userId);
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                log.LogInformation("Project fetched projectId={ProjectId} etag={ETag}",
                                    projectId, context.Response.Headers.ETag.ToString());
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.ProjectReader)
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

                var userId = (Guid)currentUserSvc.UserId!;
                var projects = await projectReadSvc.GetAllByUserAsync(userId, filter: null, ct);
                var responseDto = projects.Select(p => p.ToReadDto(userId)).ToList();

                log.LogInformation("Projects listed for current user userId={UserId} count={Count}",
                                    userId, responseDto.Count);
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

                var projects = await projectReadSvc.GetAllByUserAsync(userId, filter: null, ct);
                var responseDto = projects.Select(p => p.ToReadDto(userId)).ToList();

                log.LogInformation("Projects listed for userId={UserId} count={Count}",
                                    userId, responseDto.Count);
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.SystemAdmin)
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

                var userId = (Guid)currentUserSvc.UserId!;
                var (result, project) = await projectWriteSvc.CreateAsync(userId, dto.Name, ct);
                if (result != DomainMutation.Created || project is null)
                {
                    log.LogInformation("Project create rejected userId={UserId} mutation={Mutation}",
                                        userId, result);
                    return result.ToHttp(context);
                }

                var responseDto = project.ToReadDto(userId);
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                log.LogInformation("Project created projectId={ProjectId} ownerId={UserId} etag={ETag}",
                                    project.Id, userId, context.Response.Headers.ETag.ToString());
                return Results.CreatedAtRoute("Projects_Get_ById", new { projectId = project.Id }, responseDto);
            })
            .RequireValidation<ProjectCreateDto>()
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

                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => projectReadSvc.GetAsync(projectId, ct), p => p.RowVersion);

                var result = await projectWriteSvc.RenameAsync(projectId, dto.NewName, rowVersion, ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation("Project rename rejected projectId={ProjectId} mutation={Mutation}",
                                        projectId, result);
                    return result.ToHttp(context);
                }

                var edited = await projectReadSvc.GetAsync(projectId, ct);
                if (edited is null)
                {
                    log.LogInformation("Project rename readback missing projectId={ProjectId}", projectId);
                    return Results.NotFound();
                }

                var userId = (Guid)currentUserSvc.UserId!;
                var responseDto = edited.ToReadDto(userId);
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                log.LogInformation("Project renamed projectId={ProjectId} newName={NewName} etag={ETag}",
                                    projectId, dto.NewName, context.Response.Headers.ETag.ToString());
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireValidation<ProjectRenameDto>()
            .RequireIfMatch()
            .Produces<ProjectReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Rename project")
            .WithDescription("Updates the project name using optimistic concurrency (If-Match). Returns the updated resource and ETag.")
            .WithName("Projects_Rename");

            // DELETE /projects/{projectId}
            group.MapDelete("/{projectId:guid}", async (
                [FromRoute] Guid projectId,
                [FromServices] ILoggerFactory logger,
                [FromServices] IProjectWriteService projectWriteSvc,
                [FromServices] IProjectReadService projectReadSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Projects.Delete");

                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => projectReadSvc.GetAsync(projectId, ct), p => p.RowVersion);

                var result = await projectWriteSvc.DeleteAsync(projectId, rowVersion, ct);

                log.LogInformation("Project delete result projectId={ProjectId} mutation={Mutation}",
                                    projectId, result);
                return result.ToHttp(context);
            })
            .RequireAuthorization(Policies.ProjectOwner)
            .RequireIfMatch()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Delete project")
            .WithDescription("Owner-only. Deletes the project using optimistic concurrency (If-Match).")
            .WithName("Projects_Delete");

            return group;
        }
    }
}
