using Api.Filters;
using Api.HttpMapping;
using Application.Abstractions.Security;
using Application.Auth.DTOs;
using Application.Auth.Mapping;
using Application.Common.Exceptions;
using Application.Users.Abstractions;
using Application.Users.DTOs;
using Application.Users.Mapping;
using Domain.Enums;
using Domain.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

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
                [FromServices] IPasswordHasher hasher,
                [FromServices] IJwtTokenService jwtSvc,
                [FromServices] ILoggerFactory logger,
                [FromServices] IUserWriteService userWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Auth.Register");

                // Derive password hash and per-user salt
                var (hash, salt) = hasher.Hash(dto.Password);

                var email = Email.Create(dto.Email);
                var userName = UserName.Create(dto.Name);

                // Create the user in the write-side service using domain VOs for Email and UserName
                // Default role is User. Returns a DomainMutation and the created entity
                var (result, user) = await userWriteSvc.CreateAsync(
                    email,
                    userName,
                    hash,
                    salt,
                    UserRole.User,
                    ct);

                // If the mutation is not Created, map the domain result to an HTTP response
                if (result != DomainMutation.Created || user is null)
                {
                    // Log only a hash of the email to avoid PII leakage in logs
                    log.LogInformation(
                        "Register failed mutation={Mutation} emailHash={EmailHash}",
                        result,
                        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dto.Email ?? ""))));
                    return result.ToHttp(context);
                }

                // Issue a short-lived JWT for immediate auth. Embed subject, email, name, and role claims
                var (accessToken, expiresAtUtc) = jwtSvc.CreateToken(
                    user.Id,
                    user.Email.Value,
                    user.Name.Value,
                    user.Role);

                // Shape the response with token and its UTC expiration time
                var responseDto = user.ToReadDto(accessToken, expiresAtUtc);

                log.LogInformation(
                    "Register succeeded userId={UserId} role={Role} tokenExpUtc={TokenExpUtc}",
                    user.Id,
                    user.Role,
                    expiresAtUtc);
                return Results.Ok(responseDto);
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
                [FromServices] IPasswordHasher hasher,
                [FromServices] IJwtTokenService jwtSvc,
                [FromServices] ILoggerFactory logger,
                [FromServices] IUserReadService userReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Auth.Login");

                // Add small random jitter to reduce timing side-channel reliability
                await Task.Delay(Random.Shared.Next(10, 30), ct);

                var user = await userReadSvc.GetByEmailAsync(Email.Create(dto.Email), ct);

                // Verify hashed password using stored salt and hash
                if (user is null || !hasher.Verify(dto.Password, user.PasswordSalt, user.PasswordHash))
                {
                    // Log only a deterministic hash of the email, never the raw value
                    log.LogInformation(
                        "Login failed emailHash={EmailHash}",
                        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dto.Email ?? ""))));

                    // On failure, throw a domain-specific exception; middleware maps to 401
                    throw new InvalidCredentialsException("Email or password is incorrect.");
                }

                // Produce JWT with subject and role claims for the authenticated principal
                var (accessToken, expiresAtUtc) = jwtSvc.CreateToken(
                    user.Id,
                    user.Email.Value,
                    user.Name.Value,
                    user.Role);
                var responseDto = user.ToReadDto(accessToken, expiresAtUtc);

                log.LogInformation(
                    "Login succeeded userId={UserId} role={Role} tokenExpUtc={TokenExpUtc}",
                    user.Id,
                    user.Role,
                    expiresAtUtc);
                return Results.Ok(responseDto);
            })
            .RequireValidation<UserLoginDto>()
            .Produces<AuthTokenReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("Authenticate user")
            .WithDescription("Validates credentials and returns a JWT on success.")
            .WithName("Auth_Login");

            // GET /auth/me
            group.MapGet("/me", async (
                [FromServices] ILoggerFactory logger,
                [FromServices] IUserReadService userReadSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Auth.Get_Me");

                // Extract user id from JWT: prefer 'sub' (RFC 7519) then fallback to NameIdentifier
                var sub =
                    context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                    context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Reject if the subject is missing or not a valid Guid
                if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var userId))
                {
                    log.LogInformation("Me rejected invalid claims sub={Sub}", sub);
                    throw new InvalidCredentialsException("Invalid or missing authentication claims.");
                }

                // Load the current user profile; reject if token is valid but user no longer exists
                var user = await userReadSvc.GetAsync(userId, ct);
                if (user is null)
                {
                    log.LogInformation("Me rejected user not found userId={UserId}", userId);
                    throw new InvalidCredentialsException("User not found or token invalid.");
                }

                // Return a minimal "me" DTO not containing a new token
                var responseDto = user.ToMeReadDto();

                log.LogInformation("Me succeeded userId={UserId}", user.Id);
                return Results.Ok(responseDto);
            })
            .RequireAuthorization()
            .Produces<MeReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("Get authenticated profile")
            .WithDescription("Returns the current user profile derived from JWT claims.")
            .WithName("Auth_Get_Me");

            return group;
        }
    }
}
