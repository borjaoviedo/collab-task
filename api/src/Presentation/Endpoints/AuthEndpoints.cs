using Api.Auth.DTOs;
using Api.Auth.Mapping;
using Api.Extensions;
using Application.Common.Abstractions.Security;
using Application.Common.Exceptions;
using Application.Users.Abstractions;
using Application.Users.DTOs;
using Application.Users.Mapping;
using Domain.Common.Exceptions;
using Domain.Enums;
using FluentValidation;
using Infrastructure.Data.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Api.Endpoints
{
    public static class AuthEndpoints
    {
        public static RouteGroupBuilder MapAuth(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/auth").WithTags("Auth");

            // POST /auth/register
            group.MapPost("/register", async (
                [FromBody] UserRegisterDto dto,
                [FromServices] IUserWriteService userWriteSvc,
                [FromServices] IPasswordHasher hasher,
                [FromServices] IJwtTokenService jwtSvc,
                [FromServices] ILoggerFactory loggerFactory,
                CancellationToken ct = default) =>
            {
                var log = loggerFactory.CreateLogger("Auth.Register");
                var (hash, salt) = hasher.Hash(dto.Password);

                try
                {
                    var (res, user) = await userWriteSvc.CreateAsync(dto.Email, dto.Name, hash, salt, UserRole.User, ct);
                    if (res != DomainMutation.Created || user is null) return res.ToHttp();

                    var (accessToken, expiresAtUtc) = jwtSvc.CreateToken(user.Id, user.Email.Value, user.Name.Value, user.Role.ToString());
                    return Results.Ok(user.ToReadDto(accessToken, expiresAtUtc));
                }
                catch (DbUpdateException ex) when (ex.IsUniqueViolation())
                {
                    log.LogInformation(ex, "Duplicate email or user name on register.");
                    throw new DuplicateEntityException("Could not complete registration.");
                }
            })
            .RequireValidation<UserRegisterDto>()
            .Produces<AuthTokenReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Register new user")
            .WithDescription("Creates a user and returns a JWT for auto-login")
            .WithName("Auth_Register");

            // POST /auth/login
            group.MapPost("/login", async (
                [FromBody] UserLoginDto dto,
                [FromServices] IUserReadService userReadSvc,
                [FromServices] IPasswordHasher hasher,
                [FromServices] IJwtTokenService jwtSvc,
                [FromServices] ILoggerFactory loggerFactory,
                CancellationToken ct = default) =>
            {
                var log = loggerFactory.CreateLogger("Auth.Login");

                // small jitter to reduce timing attacks
                await Task.Delay(Random.Shared.Next(10, 30), ct);

                var user = await userReadSvc.GetByEmailAsync(dto.Email, ct);
                var valid = user is not null &&
                            hasher.Verify(dto.Password, user.PasswordSalt, user.PasswordHash);

                if (!valid)
                {
                    // anonymized logging
                    var emailHash = Convert.ToHexString(
                        SHA256.HashData(Encoding.UTF8.GetBytes(dto.Email ?? "")));
                    log.LogInformation("Login failed emailHash={EmailHash}", emailHash);

                    throw new InvalidCredentialsException("Email or password is incorrect.");
                }

                log.LogInformation("Login success userId={UserId}", user!.Id);

                var (accessToken, expiresAtUtc) = jwtSvc.CreateToken(user!.Id, user.Email.Value, user.Name.Value, user.Role.ToString());
                var payload = user.ToReadDto(accessToken, expiresAtUtc);

                return Results.Ok(payload);
            })
            .RequireValidation<UserLoginDto>()
            .Produces<AuthTokenReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("Authenticate user with email and password")
            .WithDescription("Returns a JWT bearer token on successful authentication")
            .WithName("Auth_Login");

            // GET /auth/me
            group.MapGet("/me", async (
                [FromServices] IUserReadService userReadSvc,
                [FromServices] ILoggerFactory loggerFactory,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var logger = loggerFactory.CreateLogger("Auth.Me");

                var sub =
                    http.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                    http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var userId))
                {
                    logger.LogWarning("Missing or invalid 'sub' claim.");
                    throw new InvalidCredentialsException("Invalid or missing authentication claims.");
                }

                var user = await userReadSvc.GetAsync(userId, ct);
                if (user is null)
                {
                    logger.LogWarning("Authenticated user not found. userId: {UserId}", userId);
                    throw new InvalidCredentialsException("User not found or token invalid.");
                }

                var dto = user.ToMeReadDto();

                return Results.Ok(dto);
            })
            .RequireAuthorization()
            .Produces<MeReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("Auth_GetMe")
            .WithSummary("Returns the authenticated user's profile")
            .WithDescription("User profile");

            return group;
        }
    }
}
