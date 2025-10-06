using Api.Common;
using Application.ProjectMembers.Abstractions;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

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

            g.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromBody] AddMemberDto dto,
                [FromServices] IProjectMemberService svc,
                CancellationToken ct = default) =>
            {
                var res = await svc.AddAsync(projectId, dto.UserId, dto.Role, dto.JoinedAtUtc.ToUniversalTime(), ct);
                return res.ToHttp(location: $"/projects/{projectId}/members/{dto.UserId}");
            });

            g.MapPatch("/{userId:guid}/role", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromBody] ChangeMemberRoleDto dto,
                [FromServices] IProjectMemberService svc,
                CancellationToken ct = default) =>
            {
                var res = await svc.ChangeRoleAsync(projectId, userId, dto.Role, dto.RowVersion, ct);
                return res.ToHttp();
            });

            g.MapPatch("/{userId:guid}/remove", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromBody] RemoveMemberDto dto,
                [FromServices] IProjectMemberService svc,
                CancellationToken ct = default) =>
            {
                var res = await svc.RemoveAsync(projectId, userId, dto.RowVersion, dto.RemovedAtUtc.ToUniversalTime(), ct);
                return res.ToHttp();
            });

            g.MapPatch("/{userId:guid}/restore", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid userId,
                [FromBody] RestoreMemberDto dto,
                [FromServices] IProjectMemberService svc,
                CancellationToken ct = default) =>
            {
                var res = await svc.RestoreAsync(projectId, userId, dto.RowVersion, ct);
                return res.ToHttp();
            });

            return app;
        }
    }
}
