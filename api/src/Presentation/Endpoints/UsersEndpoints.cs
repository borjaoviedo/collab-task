using Api.Auth.Authorization;
using Api.Common;
using Application.Projects.Abstractions;
using Application.Projects.DTOs;
using Application.Users.Abstractions;
using Application.Users.DTOs;
using Application.Users.Mapping;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    public static class UsersEndpoints
    {
        public sealed record RenameUserDto(string Name, byte[] RowVersion);
        public sealed record ChangeRoleDto(UserRole Role, byte[] RowVersion);
        public sealed record DeleteUserDto(byte[] RowVersion);

        public static RouteGroupBuilder MapUsers(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/users")
                .WithTags("Users")
                .RequireAuthorization();

            // GET /users
            group.MapGet("/", async (
                [FromServices] IUserRepository repo,
                [FromServices] IProjectMembershipReader membership,
                CancellationToken ct = default) =>
            {
                var users = await repo.GetAllAsync(ct);
                var dto = users.Select(u => u.ToReadDto()).ToList();

                await Task.WhenAll(dto.Select(async d =>
                    d.ProjectMembershipsCount = await membership.CountActiveAsync(d.Id, ct)));

                return Results.Ok(dto);
            })
            .RequireAuthorization(Policies.SystemAdmin)
            .Produces<IEnumerable<UserReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("Get all users")
            .WithDescription("Returns all users info including RowVersion.")
            .WithName("Users_Get_All");

            // GET /users/{id}
            group.MapGet("/{id:guid}", async (
                [FromRoute] Guid id,
                [FromServices] IUserRepository repo,
                CancellationToken ct = default) =>
            {
                var u = await repo.GetByIdAsync(id, ct);
                if (u is null) return Results.NotFound();
                return Results.Ok(u.ToReadDto());
            })
            .RequireAuthorization(Policies.SystemAdmin)
            .Produces<UserReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get user by id")
            .WithDescription("Returns user info including RowVersion.")
            .WithName("Users_Get_ById");

            // PATCH /users/{id}/name
            group.MapPatch("/{id:guid}/name", async (
                [FromRoute] Guid id,
                [FromBody] RenameUserDto dto,
                [FromServices] IUserService svc,
                CancellationToken ct = default) =>
            {
                var res = await svc.RenameAsync(id, dto.Name, dto.RowVersion, ct);
                return res.ToHttp();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Rename user")
            .WithDescription("Allows an authenticated user to rename itself")
            .WithName("Users_Rename");

            // PATCH /users/{id}/role
            group.MapPatch("/{id:guid}/role", async (
                [FromRoute] Guid id,
                [FromBody] ChangeRoleDto dto,
                [FromServices] IUserService svc,
                CancellationToken ct = default) =>
            {
                var res = await svc.ChangeRoleAsync(id, dto.Role, dto.RowVersion, ct);
                return res.ToHttp();
            })
            .RequireAuthorization(Policies.SystemAdmin)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Change user role")
            .WithDescription("Requires system admin role to modify another user's role")
            .WithName("Users_Change_Role");

            // DELETE /users/{id}
            group.MapDelete("/{id:guid}", async (
                [FromRoute] Guid id,
                [FromBody] DeleteUserDto dto,
                [FromServices] IUserService svc,
                CancellationToken ct = default) =>
            {
                var res = await svc.DeleteAsync(id, dto.RowVersion, ct);
                return res.ToHttp();
            })
            .RequireAuthorization(Policies.SystemAdmin)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Delete user")
            .WithDescription("Requires system admin role to delete an existing user")
            .WithName("Users_Delete");

            return group;
        }
    }
}
