using Api.Auth.DTOs;
using Domain.Entities;

namespace Api.Auth.Mapping
{
    public static class AuthMapping
    {
        public static AuthTokenReadDto ToReadDto(this User user, string token, DateTime expiresAtUtc)
        => new()
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresAtUtc = expiresAtUtc,
            UserId = user.Id,
            Email = user.Email.Value,
            Name = user.Name.Value,
            Role = user.Role.ToString()
        };
    }
}
