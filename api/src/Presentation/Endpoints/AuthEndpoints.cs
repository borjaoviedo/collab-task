using Api.Filters;
using Application.Auth.DTOs;
using Application.Users.Abstractions;
using Application.Users.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    /// <summary>
    /// Authentication endpoints: user registration, login, and "me" profile lookup.
    /// Issues short-lived JWTs, validates credentials, and uses domain value objects,
    /// structured logging, and OpenAPI metadata. Public endpoints unless otherwise specified.
    /// </summary>
    public static class AuthEndpoints
    {
        /// <summary>
        /// Registers the /auth route group and wires endpoint handlers, validation, metadata, and names.
        /// Returns a <see cref="RouteGroupBuilder"/> so other modules can extend the group if needed.
        /// </summary>
        /// <param name="app">The endpoint route builder.</param>
        /// <returns>The configured route group.</returns>
        public static RouteGroupBuilder MapAuth(this IEndpointRouteBuilder app)
        {
            // Create '/auth' route group and tag for OpenAPI grouping
            var group = app
                        .MapGroup("/auth")
                        .WithTags("Auth");

            // OpenAPI metadata across all endpoints: ensures generated clients and API docs
            // include consistent success/error shapes and auth requirements

            // POST /auth/register
            group.MapPost("/register", async (
                [FromBody] UserRegisterDto dto,
                [FromServices] IUserWriteService userWriteSvc,
                CancellationToken ct = default) =>
            {
                var authTokenReadDto = await userWriteSvc.RegisterAsync(dto, ct);

                return Results.Ok(authTokenReadDto);
            })
            .RequireValidation<UserRegisterDto>()
            .Produces<AuthTokenReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Register user")
            .WithDescription("Creates a user and returns a JWT for immediate authentication.")
            .WithName("Auth_Register");

            // POST /auth/login
            group.MapPost("/login", async (
                [FromBody] UserLoginDto dto,
                [FromServices] IUserWriteService userWriteSvc,
                CancellationToken ct = default) =>
            {
                var authTokenReadDto = await userWriteSvc.LoginAsync(dto, ct);
                return Results.Ok(authTokenReadDto);
            })
            .RequireValidation<UserLoginDto>()
            .Produces<AuthTokenReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("Authenticate user")
            .WithDescription("Validates credentials and returns a JWT on success.")
            .WithName("Auth_Login");

            // GET /auth/me
            group.MapGet("/me", async (
                [FromServices] IUserReadService userReadSvc,
                CancellationToken ct = default) =>
            {
                var userReadDto = await userReadSvc.GetCurrentAsync(ct);
                return Results.Ok(userReadDto);
            })
            .RequireAuthorization()
            .Produces<UserReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("Get authenticated profile")
            .WithDescription("Returns the current user profile derived from JWT claims.")
            .WithName("Auth_Get_Me");

            return group;
        }
    }
}
