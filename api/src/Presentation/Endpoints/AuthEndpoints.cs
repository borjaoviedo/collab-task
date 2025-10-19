using Api.Auth.DTOs;
using Api.Auth.Mapping;
using Api.Extensions;
using Application.Common.Abstractions.Security;
using Application.Common.Exceptions;
using Application.Users.Abstractions;
using Application.Users.DTOs;
using Application.Users.Mapping;
using Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
                [FromServices] IPasswordHasher hasher,
                [FromServices] IJwtTokenService jwtSvc,
                [FromServices] IUserWriteService userWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var (hash, salt) = hasher.Hash(dto.Password);
                var (res, user) = await userWriteSvc.CreateAsync(dto.Email, dto.Name, hash, salt, UserRole.User, ct);
                if (res != DomainMutation.Created || user is null) return res.ToHttp(context);

                var (accessToken, expiresAtUtc) = jwtSvc.CreateToken(user.Id, user.Email.Value, user.Name.Value, user.Role);
                var responseDto = user.ToReadDto(accessToken, expiresAtUtc);

                return Results.Ok(responseDto);
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
                [FromServices] IPasswordHasher hasher,
                [FromServices] IJwtTokenService jwtSvc,
                [FromServices] IUserReadService userReadSvc,
                CancellationToken ct = default) =>
            {
                // small jitter to reduce timing attacks
                await Task.Delay(Random.Shared.Next(10, 30), ct);

                var user = await userReadSvc.GetByEmailAsync(dto.Email, ct);
                if (user is null || !hasher.Verify(dto.Password, user.PasswordSalt, user.PasswordHash))
                    throw new InvalidCredentialsException("Email or password is incorrect.");

                var (accessToken, expiresAtUtc) = jwtSvc.CreateToken(user.Id, user.Email.Value, user.Name.Value, user.Role);
                var responseDto = user.ToReadDto(accessToken, expiresAtUtc);

                return Results.Ok(responseDto);
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
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var sub =
                    context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                    context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var userId))
                    throw new InvalidCredentialsException("Invalid or missing authentication claims.");

                var user = await userReadSvc.GetAsync(userId, ct)
                    ?? throw new InvalidCredentialsException("User not found or token invalid.");

                var responseDto = user.ToMeReadDto();

                return Results.Ok(responseDto);
            })
            .RequireAuthorization()
            .Produces<MeReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("Returns the authenticated user's profile")
            .WithDescription("User profile")
            .WithName("Auth_Get_Me");

            return group;
        }
    }
}
