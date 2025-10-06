using Api.Auth.Authorization;
using Api.Common;
using Application.Common.Abstractions.Auth;
using Application.Projects.Abstractions;
using Application.Projects.DTOs;
using Application.Projects.Mapping;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    public static class ProjectsEndpoints
    {
        public sealed record RenameProjectDto(string Name, byte[] RowVersion);
        public sealed record DeleteProjectDto(byte[] RowVersion);

        public static RouteGroupBuilder MapProjects(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/projects")
                .WithTags("Projects")
                .RequireAuthorization();

            // GET /projects?filter=...
            group.MapGet("/", async (
                [AsParameters] ProjectFilter filter,
                [FromServices] ICurrentUserService userSvc,
                [FromServices] IProjectRepository repo,
                CancellationToken ct = default) =>
            {
                var userId = (Guid)userSvc.UserId!;
                var projects = await repo.GetByUserAsync(userId, filter, ct);
                var dto = projects.Select(p => p.ToReadDto(userId)).ToList();
                return Results.Ok(dto);
            })
            .Produces<IEnumerable<ProjectReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("Get all projects")
            .WithDescription("Returns projects where the authenticated user has at least reader permissions.")
            .WithName("Projects_Get_All");

            // GET /projects/{projectId}
            group.MapGet("/{projectId:guid}", async (
                [FromRoute] Guid projectId,
                [FromServices] ICurrentUserService userSvc,
                [FromServices] IProjectRepository repo,
                CancellationToken ct = default) =>
            {
                var res = await repo.GetByIdAsync(projectId, ct);
                if (res is null) return Results.NotFound();

                return Results.Ok(res.ToReadDto((Guid)userSvc.UserId!));
            })
            .RequireAuthorization(Policies.ProjectReader)
            .Produces<ProjectReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get project by id")
            .WithDescription("Returns the project if the authenticated user has at least reader permissions.")
            .WithName("Projects_Get_ById");

            // POST /projects
            group.MapPost("/", async (
                [FromBody] ProjectCreateDto dto,
                [FromServices] ICurrentUserService userSvc,
                [FromServices] IProjectService svc,
                CancellationToken ct = default) =>
            {
                var res = await svc.CreateAsync((Guid)userSvc.UserId!, dto.Name, DateTimeOffset.UtcNow, ct);
                return res.ToHttp(location: "/projects");
            })
            .Produces<ProjectReadDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Create new project")
            .WithDescription("Creates a new project owned by the authenticated user.")
            .WithName("Projects_Create");

            // PATCH /projects/{projectId}/name
            group.MapPatch("/{projectId:guid}/name", async (
                [FromRoute] Guid projectId,
                [FromBody] RenameProjectDto dto,
                [FromServices] IProjectService svc,
                CancellationToken ct = default) =>
            {
                var res = await svc.RenameAsync(projectId, dto.Name, dto.RowVersion, ct);
                return res.ToHttp();
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Rename project")
            .WithDescription("Requires at least admin role in the target project.")
            .WithName("Projects_Rename");

            // DELETE /projects/{projectId}
            group.MapDelete("/{projectId:guid}", async (
                [FromRoute] Guid projectId,
                [FromBody] DeleteProjectDto dto,
                [FromServices] IProjectService svc,
                CancellationToken ct = default) =>
            {
                var res = await svc.DeleteAsync(projectId, dto.RowVersion, ct);
                return res.ToHttp();
            })
            .RequireAuthorization(Policies.ProjectOwner)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Delete project")
            .WithDescription("Requires owner role in the target project.")
            .WithName("Projects_Delete");

            return group;
        }
    }
}
