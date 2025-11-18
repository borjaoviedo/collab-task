using Application.Auth.DTOs;
using Domain.Entities;

namespace Application.Auth.Mapping
{
    public static class AuthMapping
    {
        public static AuthTokenReadDto ToAuthTokenReadDto(this User user, string token, DateTime expiresAtUtc)
        => new()
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresAtUtc = expiresAtUtc,
            UserId = user.Id,
            Email = user.Email.Value,
            Name = user.Name.Value
        };
    }
}
