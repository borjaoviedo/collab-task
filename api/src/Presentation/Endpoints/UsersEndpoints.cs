using Api.Auth.Authorization;
using Api.Concurrency;
using Api.Filters;
using Api.HttpMapping;
using Application.Users.Abstractions;
using Application.Users.DTOs;
using Application.Users.Mapping;
using Domain.Enums;
using Domain.ValueObjects;
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
                [FromServices] ILoggerFactory logger,
                [FromServices] IUserReadService userReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Users.Get_All");

                // System-wide listing for administrators. Returns lightweight DTOs
                var users = await userReadSvc.ListAsync(ct);
                var responseDto = users.Select(u => u.ToReadDto()).ToList();

                log.LogInformation("Users listed count={Count}", responseDto.Count);
                return Results.Ok(responseDto);
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
                [FromServices] ILoggerFactory logger,
                [FromServices] IUserReadService userReadSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Users.Get_ById");

                // Read a single user; return 404 if not found
                var user = await userReadSvc.GetAsync(userId, ct);
                if (user is null)
                {
                    log.LogInformation("User not found userId={UserId}", userId);
                    return Results.NotFound();
                }

                // Attach weak ETag from RowVersion for conditional requests
                var responseDto = user.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "User fetched userId={UserId} etag={ETag}",
                    userId,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.SystemAdmin) // SystemAdmin-only 
            .Produces<UserReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get user")
            .WithDescription("Admin-only. Returns a user. Sets ETag.")
            .WithName("Users_Get_ById");

            // GET /users/by-email?email=
            group.MapGet("/by-email", async (
                [FromQuery] string email,
                [FromServices] ILoggerFactory logger,
                [FromServices] IUserReadService userReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Users.Get_ByEmail");

                // Admin lookup by email using domain VO normalization
                var user = await userReadSvc.GetByEmailAsync(Email.Create(email), ct);
                if (user is null)
                {
                    log.LogInformation("User not found by email email={Email}", email);
                    return Results.NotFound();
                }

                var responseDto = user.ToReadDto();

                log.LogInformation("User fetched by email email={Email}", email);
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.SystemAdmin) // SystemAdmin-only 
            .Produces<UserReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get user by email")
            .WithDescription("Admin-only. Returns a user by email.")
            .WithName("Users_Get_ByEmail");

            // PATCH /users/{userId}/rename
            group.MapPatch("/{userId:guid}/rename", async (
                [FromRoute] Guid userId,
                [FromBody] UserRenameDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] IUserReadService userReadSvc,
                [FromServices] IUserWriteService userWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Users.Rename");

                // Resolve current RowVersion from If-Match or storage to guard against lost updates
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => userReadSvc.GetAsync(userId, ct),
                    u => u.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "User not found when resolving row version userId={UserId}",
                        userId);
                    return Results.NotFound();
                }

                // Rename under optimistic concurrency; name normalized via domain VO
                var userName = UserName.Create(dto.NewName);
                var result = await userWriteSvc.RenameAsync(userId, userName, rowVersion, ct);

                if (result != DomainMutation.Updated)
                {
                    log.LogInformation(
                        "User rename rejected userId={UserId} mutation={Mutation}",
                        userId,
                        result);
                    return result.ToHttp(context);
                }

                // Read back to return fresh state and a new ETag
                var renamed = await userReadSvc.GetAsync(userId, ct);
                if (renamed is null)
                {
                    log.LogInformation("User rename readback missing userId={UserId}", userId);
                    return Results.NotFound();
                }

                var responseDto = renamed.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "User renamed userId={UserId} newName={NewName} etag={ETag}",
                    userId,
                    dto.NewName,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
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
                [FromServices] ILoggerFactory logger,
                [FromServices] IUserReadService userReadSvc,
                [FromServices] IUserWriteService userWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Users.ChangeRole");

                // Resolve RowVersion and apply role change under optimistic concurrency
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => userReadSvc.GetAsync(userId, ct),
                    u => u.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation("User not found when resolving row version userId={UserId}", userId);
                    return Results.NotFound();
                }

                var result = await userWriteSvc.ChangeRoleAsync(userId, dto.NewRole, rowVersion, ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation(
                        "User role change rejected userId={UserId} mutation={Mutation}",
                        userId,
                        result);
                    return result.ToHttp(context);
                }

                // Return updated representation with refreshed ETag
                var edited = await userReadSvc.GetAsync(userId, ct);
                if (edited is null)
                {
                    log.LogInformation("User role change readback missing userId={UserId}", userId);
                    return Results.NotFound();
                }

                var responseDto = edited.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "User role changed userId={UserId} newRole={NewRole} etag={ETag}",
                    userId,
                    dto.NewRole,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
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
                [FromServices] ILoggerFactory logger,
                [FromServices] IUserReadService userReadSvc,
                [FromServices] IUserWriteService userWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Users.Delete");

                // Conditional delete of a user; map DomainMutation to HTTP (204, 404, 409, 412)
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => userReadSvc.GetAsync(userId, ct),
                    u => u.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation("User not found when resolving row version userId={UserId}", userId);
                    return Results.NotFound();
                }

                var result = await userWriteSvc.DeleteAsync(userId, rowVersion, ct);

                log.LogInformation(
                    "User delete result userId={UserId} mutation={Mutation}",
                    userId,
                    result);
                return result.ToHttp(context);
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
