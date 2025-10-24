using Api.Auth.DTOs;
using Api.Auth.Mapping;
using Api.Extensions;
using Application.Common.Abstractions.Security;
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
    public static class AuthEndpoints
    {
        public static RouteGroupBuilder MapAuth(this IEndpointRouteBuilder app)
        {
            var group = app
                        .MapGroup("/auth")
                        .WithTags("Auth");

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

                var (hash, salt) = hasher.Hash(dto.Password);
                var (result, user) = await userWriteSvc.CreateAsync(
                    Email.Create(dto.Email),
                    UserName.Create(dto.Name),
                    hash,
                    salt,
                    UserRole.User,
                    ct);

                if (result != DomainMutation.Created || user is null)
                {
                    log.LogInformation("Register failed mutation={Mutation} emailHash={EmailHash}",
                                        result, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dto.Email ?? ""))));
                    return result.ToHttp(context);
                }

                var (accessToken, expiresAtUtc) = jwtSvc.CreateToken(user.Id, user.Email.Value, user.Name.Value, user.Role);
                var responseDto = user.ToReadDto(accessToken, expiresAtUtc);

                log.LogInformation("Register succeeded userId={UserId} role={Role} tokenExpUtc={TokenExpUtc}",
                                    user.Id, user.Role, expiresAtUtc);
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

                await Task.Delay(Random.Shared.Next(10, 30), ct); // small jitter to reduce timing attacks

                var user = await userReadSvc.GetByEmailAsync(Email.Create(dto.Email), ct);

                if (user is null || !hasher.Verify(dto.Password, user.PasswordSalt, user.PasswordHash))
                {
                    log.LogInformation("Login failed emailHash={EmailHash}",
                                        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dto.Email ?? ""))));
                    throw new InvalidCredentialsException("Email or password is incorrect.");
                }

                var (accessToken, expiresAtUtc) = jwtSvc.CreateToken(user.Id, user.Email.Value, user.Name.Value, user.Role);
                var responseDto = user.ToReadDto(accessToken, expiresAtUtc);

                log.LogInformation("Login succeeded userId={UserId} role={Role} tokenExpUtc={TokenExpUtc}",
                                    user.Id, user.Role, expiresAtUtc);
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

                var sub =
                    context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                    context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var userId))
                {
                    log.LogInformation("Me rejected invalid claims sub={Sub}", sub);
                    throw new InvalidCredentialsException("Invalid or missing authentication claims.");
                }

                var user = await userReadSvc.GetAsync(userId, ct);
                if (user is null)
                {
                    log.LogInformation("Me rejected user not found userId={UserId}", userId);
                    throw new InvalidCredentialsException("User not found or token invalid.");
                }

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
