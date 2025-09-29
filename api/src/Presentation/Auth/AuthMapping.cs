using Domain.Entities;

namespace Api.Auth
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
            Role = user.Role.ToString()
        };
    }
}
