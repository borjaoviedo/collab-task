using Api.Auth.Authorization;
using Api.Extensions;
using Api.Filters;
using Application.ProjectMembers.Abstractions;
using Application.Users.Abstractions;
using Application.Users.DTOs;
using Application.Users.Mapping;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    public static class UsersEndpoints
    {
        public static RouteGroupBuilder MapUsers(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/users")
                .WithTags("Users")
                .RequireAuthorization();

            // GET /users
            group.MapGet("/", async (
                [FromServices] IUserReadService userReadSvc,
                [FromServices] IProjectMemberReadService projectMemberReadSvc,
                CancellationToken ct = default) =>
            {
                var users = await userReadSvc.ListAsync(ct);
                var dto = users.Select(u => u.ToReadDto()).ToList();

                foreach (var d in dto)
                {
                    d.ProjectMembershipsCount = await projectMemberReadSvc.CountActiveAsync(d.Id, ct);
                }

                return Results.Ok(dto);
            })
            .RequireAuthorization(Policies.SystemAdmin)
            .Produces<IEnumerable<UserReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("Get all users")
            .WithDescription("Lists all users with summary info and active project membership counts.")
            .WithName("Users_Get_All");

            // GET /users/{userId}
            group.MapGet("/{userId:guid}", async (
                [FromRoute] Guid userId,
                [FromServices] IUserReadService svc,
                CancellationToken ct = default) =>
            {
                var u = await svc.GetAsync(userId, ct);
                if (u is null) return Results.NotFound();
                return Results.Ok(u.ToReadDto());
            })
            .RequireAuthorization(Policies.SystemAdmin)
            .Produces<UserReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get user by id")
            .WithDescription("Gets a user by id. Returns summary info.")
            .WithName("Users_Get_ById");

            // GET /users/by-email?email=
            group.MapGet("/by-email", async (
                [FromQuery] string email,
                [FromServices] IUserReadService userReadSvc,
                CancellationToken ct = default) =>
            {
                var user = await userReadSvc.GetByEmailAsync(email, ct);
                return user is null ? Results.NotFound() : Results.Ok(user.ToReadDto());
            })
            .RequireAuthorization(Policies.SystemAdmin)
            .Produces<UserReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get user by email")
            .WithDescription("Returns a user by email. Returns summary info.")
            .WithName("Users_Get_ByEmail");

            // PATCH /users/{userId}/rename
            group.MapPatch("/{userId:guid}/rename", async (
                [FromRoute] Guid userId,
                [FromBody] UserRenameDto dto,
                [FromServices] IUserReadService userReadSvc,
                [FromServices] IUserWriteService userWriteSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = (byte[])http.Items["rowVersion"]!;
                var result = await userWriteSvc.RenameAsync(userId, dto.NewName, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(http);

                var renamed = await userReadSvc.GetAsync(userId, ct);
                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(renamed!.RowVersion)}\"";
                return Results.Ok(renamed.ToReadDto());
            })
            .AddEndpointFilter<IfMatchRowVersionFilter>()
            .RequireAuthorization()
            .Produces<UserReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Rename user")
            .WithDescription("Renames a user and returns the updated user.")
            .WithName("Users_Rename");

            // PATCH /users/{userId}/role
            group.MapPatch("/{userId:guid}/role", async (
                [FromRoute] Guid userId,
                [FromBody] UserChangeRoleDto dto,
                [FromServices] IUserReadService userReadSvc,
                [FromServices] IUserWriteService userWriteSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = (byte[])http.Items["rowVersion"]!;
                var result = await userWriteSvc.ChangeRoleAsync(userId, dto.NewRole, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(http);

                var edited = await userReadSvc.GetAsync(userId, ct);
                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(edited!.RowVersion)}\"";
                return Results.Ok(edited.ToReadDto());
            })
            .AddEndpointFilter<IfMatchRowVersionFilter>()
            .RequireAuthorization(Policies.SystemAdmin)
            .Produces<UserReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Change user role")
            .WithDescription("Changes a user's role and returns the updated user.")
            .WithName("Users_Change_Role");

            // DELETE /users/{userId}
            group.MapDelete("/{userId:guid}", async (
                [FromRoute] Guid userId,
                [FromServices] IUserWriteService svc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = (byte[])http.Items["rowVersion"]!;
                var res = await svc.DeleteAsync(userId, rowVersion, ct);
                return res.ToHttp(http);
            })
            .AddEndpointFilter<IfMatchRowVersionFilter>()
            .RequireAuthorization(Policies.SystemAdmin)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Delete user")
            .WithDescription("Deletes an existing user.")
            .WithName("Users_Delete");

            return group;
        }
    }
}
