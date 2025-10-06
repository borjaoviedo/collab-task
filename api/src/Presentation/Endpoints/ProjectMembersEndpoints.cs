using Api.Common;
using Application.ProjectMembers.Abstractions;
using Domain.Enums;

namespace Api.Endpoints
{
    public static class ProjectMembersEndpoints
    {
        public sealed record AddMemberDto(Guid UserId, ProjectRole Role, DateTimeOffset JoinedAtUtc);
        public sealed record ChangeMemberRoleDto(ProjectRole Role, byte[] RowVersion);
        public sealed record RemoveMemberDto(byte[] RowVersion, DateTimeOffset RemovedAtUtc);
        public sealed record RestoreMemberDto(byte[] RowVersion);

        public static IEndpointRouteBuilder MapProjectMembers(this IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/projects/{projectId:guid}/members").WithTags("Project Members");

            g.MapPost("/", async (Guid projectId, AddMemberDto dto, IProjectMemberService svc, CancellationToken ct) =>
            {
                var res = await svc.AddAsync(projectId, dto.UserId, dto.Role, dto.JoinedAtUtc.ToUniversalTime(), ct);
                return res.ToHttp(location: $"/projects/{projectId}/members/{dto.UserId}");
            });

            g.MapPatch("/{userId:guid}/role", async (Guid projectId, Guid userId, ChangeMemberRoleDto dto, IProjectMemberService svc, CancellationToken ct) =>
            {
                var res = await svc.ChangeRoleAsync(projectId, userId, dto.Role, dto.RowVersion, ct);
                return res.ToHttp();
            });

            g.MapPatch("/{userId:guid}/remove", async (Guid projectId, Guid userId, RemoveMemberDto dto, IProjectMemberService svc, CancellationToken ct) =>
            {
                var res = await svc.RemoveAsync(projectId, userId, dto.RowVersion, dto.RemovedAtUtc.ToUniversalTime(), ct);
                return res.ToHttp();
            });

            g.MapPatch("/{userId:guid}/restore", async (Guid projectId, Guid userId, RestoreMemberDto dto, IProjectMemberService svc, CancellationToken ct) =>
            {
                var res = await svc.RestoreAsync(projectId, userId, dto.RowVersion, ct);
                return res.ToHttp();
            });

            return app;
        }
    }
}
