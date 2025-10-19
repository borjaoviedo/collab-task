using Api.Auth.Authorization;
using Api.Extensions;
using Api.Helpers;
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
                CancellationToken ct = default) =>
            {
                var users = await userReadSvc.ListAsync(ct);
                var responseDto = users.Select(u => u.ToReadDto()).ToList();

                return Results.Ok(responseDto);
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
                [FromServices] IUserReadService userReadSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var user = await userReadSvc.GetAsync(userId, ct);
                if (user is null) return Results.NotFound();

                var responseDto = user.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                return Results.Ok(responseDto);
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
                if (user is null) return Results.NotFound();

                var responseDto = user.ToReadDto();

                return Results.Ok(responseDto);
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
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => userReadSvc.GetAsync(userId, ct), u => u.RowVersion);

                var result = await userWriteSvc.RenameAsync(userId, dto.NewName, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(context);

                var renamed = await userReadSvc.GetAsync(userId, ct);
                if (renamed is null) return Results.NotFound();

                var responseDto = renamed.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                return Results.Ok(responseDto);
            })
            .RequireValidation<UserRenameDto>()
            .RequireIfMatch()
            .Produces<UserReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Rename user")
            .WithDescription("Renames a user and returns the updated user.")
            .WithName("Users_Rename");

            // PATCH /users/{userId}/role
            group.MapPatch("/{userId:guid}/role", async (
                [FromRoute] Guid userId,
                [FromBody] UserChangeRoleDto dto,
                [FromServices] IUserReadService userReadSvc,
                [FromServices] IUserWriteService userWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => userReadSvc.GetAsync(userId, ct), u => u.RowVersion);

                var result = await userWriteSvc.ChangeRoleAsync(userId, dto.NewRole, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(context);

                var edited = await userReadSvc.GetAsync(userId, ct);
                if (edited is null) return Results.NotFound();

                var responseDto = edited.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.SystemAdmin)
            .RequireValidation<UserChangeRoleDto>()
            .RequireIfMatch()
            .Produces<UserReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Change user role")
            .WithDescription("Changes a user's role and returns the updated user.")
            .WithName("Users_ChangeRole");

            // DELETE /users/{userId}
            group.MapDelete("/{userId:guid}", async (
                [FromRoute] Guid userId,
                [FromServices] IUserReadService userReadSvc,
                [FromServices] IUserWriteService userWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => userReadSvc.GetAsync(userId, ct), u => u.RowVersion);

                var result = await userWriteSvc.DeleteAsync(userId, rowVersion, ct);

                return result.ToHttp(context);
            })
            .RequireAuthorization(Policies.SystemAdmin)
            .RequireIfMatch()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Delete user")
            .WithDescription("Deletes an existing user.")
            .WithName("Users_Delete");

            return group;
        }
    }
}
