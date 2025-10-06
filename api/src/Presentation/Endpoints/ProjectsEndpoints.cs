using Api.Common;
using Application.Projects.Abstractions;

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

            g.MapPost("/", async (IProjectService svc, CreateProjectDto dto, CancellationToken ct) =>
            {
                var res = await svc.CreateAsync(dto.OwnerId, dto.Name, DateTimeOffset.UtcNow, ct);
                return res.ToHttp(location: $"/projects");
            });

            g.MapPatch("/{id:guid}/name", async (Guid id, RenameProjectDto dto, IProjectService svc, CancellationToken ct) =>
            {
                var res = await svc.RenameAsync(id, dto.Name, dto.RowVersion, ct);
                return res.ToHttp();
            });

            g.MapDelete("/{id:guid}", async (Guid id, DeleteProjectDto dto, IProjectService svc, CancellationToken ct) =>
            {
                var res = await svc.DeleteAsync(id, dto.RowVersion, ct);
                return res.ToHttp();
            });

            return app;
        }
    }
}
