using Api.Auth.Authorization;
using Api.Concurrency;
using Api.Filters;
using Application.Users.Abstractions;
using Application.Users.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    /// <summary>
    /// User administration endpoints: list, read, rename, change role, and delete.
    /// Uses per-endpoint authorization and optimistic concurrency via ETag/If-Match.
    /// </summary>
    public static class UsersEndpoints
    {
        /// <summary>
        /// Registers system user endpoints under /users and wires handlers,
        /// validation, authorization, and OpenAPI metadata.
        /// </summary>
        /// <param name="app">Endpoint route builder.</param>
        /// <returns>The configured route group.</returns>
        public static RouteGroupBuilder MapUsers(this IEndpointRouteBuilder app)
        {
            // Group user admin endpoints; default requires authentication
            var group = app
                .MapGroup("/users")
                .WithTags("Users")
                .RequireAuthorization();

            // OpenAPI metadata across all endpoints: ensures generated clients and API docs
            // include consistent success/error shapes, auth requirements, and concurrency responses

            // GET /users
            group.MapGet("/", async (
                [FromServices] IUserReadService userReadSvc,
                CancellationToken ct = default) =>
            {
                var userReadDtoList = await userReadSvc.ListAsync(ct);
                return Results.Ok(userReadDtoList);
            })
            .RequireAuthorization(Policies.SystemAdmin) // SystemAdmin-only 
            .Produces<IEnumerable<UserReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List users")
            .WithDescription("Admin-only. Returns users with summary info and active membership counts.")
            .WithName("Users_Get_All");

            // GET /users/{userId}
            group.MapGet("/{userId:guid}", async (
                [FromRoute] Guid userId,
                [FromServices] IUserReadService userReadSvc,
                CancellationToken ct = default) =>
            {
                var userReadDto = await userReadSvc.GetByIdAsync(userId, ct);
                var etag = ETag.EncodeWeak(userReadDto.RowVersion);

                return Results.Ok(userReadDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.SystemAdmin) // SystemAdmin-only 
            .Produces<UserReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get user")
            .WithDescription("Admin-only. Returns a user. Sets ETag.")
            .WithName("Users_Get_ById");

            // PATCH /users/{userId}/rename
            group.MapPatch("/{userId:guid}/rename", async (
                [FromRoute] Guid userId,
                [FromBody] UserRenameDto dto,
                [FromServices] IUserWriteService userWriteSvc,
                CancellationToken ct = default) =>
            {
                var renamedUserReadDto = await userWriteSvc.RenameAsync(dto, ct);
                var etag = ETag.EncodeWeak(renamedUserReadDto.RowVersion);

                return Results.Ok(renamedUserReadDto).WithETag(etag);
            })
            .RequireValidation<UserRenameDto>()
            .RequireIfMatch() // Require If-Match to prevent overwriting concurrent edits
            .Produces<UserReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Rename user")
            .WithDescription("Updates the user name using optimistic concurrency (If-Match). Returns the updated resource and ETag.")
            .WithName("Users_Rename");

            // PATCH /users/{userId}/role
            group.MapPatch("/{userId:guid}/role", async (
                [FromRoute] Guid userId,
                [FromBody] UserChangeRoleDto dto,
                [FromServices] IUserWriteService userWriteSvc,
                CancellationToken ct = default) =>
            {
                var editedUserReadDto = await userWriteSvc.ChangeRoleAsync(userId, dto, ct);
                var etag = ETag.EncodeWeak(editedUserReadDto.RowVersion);

                return Results.Ok(editedUserReadDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.SystemAdmin) // SystemAdmin-only
            .RequireValidation<UserChangeRoleDto>()
            .RequireIfMatch() // Requires If-Match to avoid lost updates 
            .Produces<UserReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Change user role")
            .WithDescription("Admin-only. Changes role using optimistic concurrency (If-Match). Returns the updated resource and ETag.")
            .WithName("Users_ChangeRole");

            // DELETE /users/{userId}
            group.MapDelete("/{userId:guid}", async (
                [FromRoute] Guid userId,
                [FromServices] IUserWriteService userWriteSvc,
                CancellationToken ct = default) =>
            {
                await userWriteSvc.DeleteByIdAsync(userId, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Policies.SystemAdmin) // SystemAdmin-only
            .RequireIfMatch() // Requires If-Match to prevent deleting over stale state
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Delete user")
            .WithDescription("Admin-only. Deletes a user using optimistic concurrency (If-Match).")
            .WithName("Users_Delete");

            return group;
        }
    }
}
