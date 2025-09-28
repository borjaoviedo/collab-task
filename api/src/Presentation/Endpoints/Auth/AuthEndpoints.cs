using Api.Auth;
using Application.Common.Abstractions.Persistence;
using Application.Common.Abstractions.Security;
using Application.Common.Exceptions;
using Application.Users.Abstractions;
using Application.Users.DTOs;
using Application.Users.Mapping;
using Infrastructure.Data.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Auth
{
    public static class AuthEndpoints
    {
        public static RouteGroupBuilder MapAuth(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/auth").WithTags("Auth");

            // POST /auth/register
            group.MapPost("/register", async (
                [FromServices] IUserRepository users,
                [FromServices] IPasswordHasher hasher,
                [FromServices] IJwtTokenService jwt,
                [FromServices] IUnitOfWork uow,
                [FromServices] ILoggerFactory loggerFactory,
                [FromBody] UserCreateDto dto,
                CancellationToken ct = default) =>
            {
                var log = loggerFactory.CreateLogger("Auth.Register");

                if (await users.ExistsByEmailAsync(dto.Email, ct))
                    throw new DuplicateEntityException("Could not complete registration.");

                var (hash, salt) = hasher.Hash(dto.Password);
                var user = UserMapping.ToEntity(dto, hash, salt);

                try
                {
                    await users.CreateAsync(user, ct);
                    await uow.SaveChangesAsync(ct);
                }
                catch (DbUpdateException ex) when (ex.IsUniqueViolation())
                {
                    log.LogInformation(ex, "Duplicate email on register.");
                    throw new DuplicateEntityException("Could not complete registration.");
                }

                var token = jwt.CreateToken(user.Id, user.Email.Value, user.Role.ToString());

                var payload = new AuthTokenReadDto
                {
                    AccessToken = token,
                    UserId = user.Id,
                    Email = user.Email.Value,
                    Role = user.Role.ToString()
                };

                return Results.Ok(payload);
            })
            .Produces<AuthTokenReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Register new user")
            .WithDescription("Creates a user and returns a JWT for auto-login")
            .WithName("Auth_Register");

            return group;
        }
    }
}
