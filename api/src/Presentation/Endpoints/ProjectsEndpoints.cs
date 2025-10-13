using Api.Auth.Authorization;
using Api.Extensions;
using Api.Filters;
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
                [FromServices] ICurrentUserService userSvc,
                [FromServices] IProjectReadService projectReadSvc,
                CancellationToken ct = default) =>
            {
                var userId = (Guid)userSvc.UserId!;
                var projects = await projectReadSvc.GetAllByUserAsync(userId, filter, ct);
                var dto = projects.Select(p => p.ToReadDto(userId)).ToList();
                return Results.Ok(dto);
            })
            .Produces<IEnumerable<ProjectReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("Get all projects")
            .WithDescription("Returns all projects where the authenticated user has at least reader permissions.")
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
            .WithDescription("Returns the specified project if the authenticated user has at least reader permissions.")
            .WithName("Projects_Get_ById");

            // GET /projects/me
            group.MapGet("/me", async (
                HttpContext http,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] IProjectReadService projectReadSvc,
                CancellationToken ct = default) =>
            {
                var userId = (Guid)currentUserSvc.UserId!;
                var projects = await projectReadSvc.GetAllByUserAsync(userId, filter: null, ct);

                var dto = projects.Select(p => p.ToReadDto(userId)).ToList();
                return Results.Ok(dto);
            })
            .RequireAuthorization(Policies.ProjectReader)
            .Produces<IEnumerable<ProjectReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("List my projects")
            .WithDescription("Lists all projects the authenticated user can access.")
            .WithName("Projects_ListMine");

            // GET /projects/users/{userId}
            group.MapGet("/users/{userId:guid}", async (
                [FromRoute] Guid userId,
                [FromServices] IProjectReadService projectReadSvc,
                CancellationToken ct = default) =>
            {
                var projects = await projectReadSvc.GetAllByUserAsync(userId, filter: null, ct);

                var dto = projects.Select(p => p.ToReadDto(userId)).ToList();
                return Results.Ok(dto);
            })
            .RequireAuthorization(Policies.ProjectReader)
            .Produces<IEnumerable<ProjectReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List projects by user")
            .WithDescription("Lists all projects the specified user can access. Admin-only if the user differs from the caller.")
            .WithName("Projects_List_ByUser");

            // POST /projects
            group.MapPost("/", async (
                [FromBody] ProjectCreateDto dto,
                [FromServices] ICurrentUserService userSvc,
                [FromServices] IProjectWriteService projectWriteSvc,
                [FromServices] IProjectReadService projectReadSvc,
                CancellationToken ct = default) =>
            {
                var userId = (Guid)userSvc.UserId!;
                var (result, project) = await projectWriteSvc.CreateAsync(userId, dto.Name, DateTimeOffset.UtcNow, ct);
                if (result != DomainMutation.Created) return result.ToHttp();

                var created = await projectReadSvc.GetAsync(project!.Id, ct);
                if (created is null)
                    return Results.Problem(statusCode: 500, title: "Could not load created project");

                var body = created.ToReadDto(userId);
                return Results.Created($"/projects/{project.Id}", body);
            })
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
                [FromServices] ICurrentUserService userSvc,
                [FromServices] IProjectWriteService projectWriteSvc,
                [FromServices] IProjectReadService projectReadSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = (byte[])http.Items["rowVersion"]!;
                var result = await projectWriteSvc.RenameAsync(projectId, dto.NewName, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(http);

                var edited = await projectReadSvc.GetAsync(projectId, ct);
                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(edited!.RowVersion)}\"";
                return Results.Ok(edited.ToReadDto((Guid)userSvc.UserId!));
            })
            .AddEndpointFilter<IfMatchRowVersionFilter>()
            .RequireAuthorization(Policies.ProjectAdmin)
            .Produces<ProjectReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Rename project")
            .WithDescription("Renames an existing project.")
            .WithName("Projects_Rename");

            // DELETE /projects/{projectId}
            group.MapDelete("/{projectId:guid}", async (
                [FromRoute] Guid projectId,
                [FromServices] IProjectWriteService svc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = (byte[])http.Items["rowVersion"]!;
                var res = await svc.DeleteAsync(projectId, rowVersion, ct);
                return res.ToHttp(http);
            })
            .AddEndpointFilter<IfMatchRowVersionFilter>()
            .RequireAuthorization(Policies.ProjectOwner)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Delete project")
            .WithDescription("Deletes the specified project.")
            .WithName("Projects_Delete");

            return group;
        }
    }
}
