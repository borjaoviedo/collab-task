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
            var group = app.MapGroup("/projects")
                .WithTags("Projects")
                .RequireAuthorization();

            // GET /projects?filter=...
            group.MapGet("/", async (
                [AsParameters] ProjectFilter filter,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] IProjectReadService projectReadSvc,
                CancellationToken ct = default) =>
            {
                var userId = (Guid)currentUserSvc.UserId!;
                var projects = await projectReadSvc.GetAllByUserAsync(userId, filter, ct);
                var responseDto = projects.Select(p => p.ToReadDto(userId)).ToList();

                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<ProjectReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("Get all projects")
            .WithDescription("Returns all projects where the authenticated user has at least reader permissions.")
            .WithName("Projects_Get_All");

            // GET /projects/{projectId}
            group.MapGet("/{projectId:guid}", async (
                [FromRoute] Guid projectId,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] IProjectRepository projectRepository,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var project = await projectRepository.GetByIdAsync(projectId, ct);
                if (project is null) return Results.NotFound();

                var userId = (Guid)currentUserSvc.UserId!;
                var responseDto = project.ToReadDto(userId);
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.ProjectReader)
            .Produces<ProjectReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get project by id")
            .WithDescription("Returns the specified project if the authenticated user has at least reader permissions.")
            .WithName("Projects_Get_ById");

            // GET /projects/me
            group.MapGet("/me", async (
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] IProjectReadService projectReadSvc,
                CancellationToken ct = default) =>
            {
                var userId = (Guid)currentUserSvc.UserId!;
                var projects = await projectReadSvc.GetAllByUserAsync(userId, filter: null, ct);
                var responseDto = projects.Select(p => p.ToReadDto(userId)).ToList();

                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<ProjectReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("List my projects")
            .WithDescription("Lists all projects the authenticated user can access.")
            .WithName("Projects_Get_Mine");

            // GET /projects/users/{userId}
            group.MapGet("/users/{userId:guid}", async (
                [FromRoute] Guid userId,
                [FromServices] IProjectReadService projectReadSvc,
                CancellationToken ct = default) =>
            {
                var projects = await projectReadSvc.GetAllByUserAsync(userId, filter: null, ct);
                var responseDto = projects.Select(p => p.ToReadDto(userId)).ToList();

                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.SystemAdmin)
            .Produces<IEnumerable<ProjectReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List projects by user")
            .WithDescription("Lists all projects the specified user can access.")
            .WithName("Projects_Get_ByUser");

            // POST /projects
            group.MapPost("/", async (
                [FromBody] ProjectCreateDto dto,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] IProjectWriteService projectWriteSvc,
                [FromServices] IProjectReadService projectReadSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var userId = (Guid)currentUserSvc.UserId!;
                var (result, project) = await projectWriteSvc.CreateAsync(userId, dto.Name, ct);
                if (result != DomainMutation.Created || project is null) return result.ToHttp(context);

                var responseDto = project.ToReadDto(userId);
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                return Results.CreatedAtRoute(
                    "Projects_Get_ById",
                    new { projectId = project.Id},
                    responseDto);
            })
            .RequireValidation<ProjectCreateDto>()
            .Produces<ProjectReadDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Create new project")
            .WithDescription("Creates a new project owned by the authenticated user.")
            .WithName("Projects_Create");

            // PATCH /projects/{projectId}/rename
            group.MapPatch("/{projectId:guid}/rename", async (
                [FromRoute] Guid projectId,
                [FromBody] ProjectRenameDto dto,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] IProjectReadService projectReadSvc,
                [FromServices] IProjectWriteService projectWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => projectReadSvc.GetAsync(projectId, ct), p => p.RowVersion);

                var result = await projectWriteSvc.RenameAsync(projectId, dto.NewName, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(context);

                var edited = await projectReadSvc.GetAsync(projectId, ct);
                if (edited is null) return Results.NotFound();

                var userId = (Guid)currentUserSvc.UserId!;
                var responseDto = edited.ToReadDto(userId);
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

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
            .WithDescription("Renames an existing project.")
            .WithName("Projects_Rename");

            // DELETE /projects/{projectId}
            group.MapDelete("/{projectId:guid}", async (
                [FromRoute] Guid projectId,
                [FromServices] IProjectWriteService projectWriteSvc,
                [FromServices] IProjectReadService projectReadSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => projectReadSvc.GetAsync(projectId, ct), p => p.RowVersion);

                var result = await projectWriteSvc.DeleteAsync(projectId, rowVersion, ct);

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
            .WithDescription("Deletes the specified project.")
            .WithName("Projects_Delete");

            return group;
        }
    }
}
