using Api.Common;
using Application.Projects.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    public static class ProjectsEndpoints
    {
        public sealed record CreateProjectDto(Guid OwnerId, string Name);
        public sealed record RenameProjectDto(string Name, byte[] RowVersion);
        public sealed record DeleteProjectDto(byte[] RowVersion);

        public static IEndpointRouteBuilder MapProjects(this IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/projects").WithTags("Projects");

            g.MapPost("/", async (
                [FromBody] CreateProjectDto dto,
                [FromServices] IProjectService svc,
                CancellationToken ct = default) =>
            {
                var res = await svc.CreateAsync(dto.OwnerId, dto.Name, DateTimeOffset.UtcNow, ct);
                return res.ToHttp(location: $"/projects");
            });

            g.MapPatch("/{id:guid}/name", async (
                [FromRoute] Guid id,
                [FromBody] RenameProjectDto dto,
                [FromServices] IProjectService svc,
                CancellationToken ct = default) =>
            {
                var res = await svc.RenameAsync(id, dto.Name, dto.RowVersion, ct);
                return res.ToHttp();
            });

            g.MapDelete("/{id:guid}", async (
                [FromRoute] Guid id,
                [FromBody] DeleteProjectDto dto,
                [FromServices] IProjectService svc,
                CancellationToken ct = default) =>
            {
                var res = await svc.DeleteAsync(id, dto.RowVersion, ct);
                return res.ToHttp();
            });

            return app;
        }
    }
}
