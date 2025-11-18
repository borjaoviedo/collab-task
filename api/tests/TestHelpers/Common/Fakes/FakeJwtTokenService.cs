using Application.Abstractions.Security;
using Domain.Enums;

namespace TestHelpers.Common.Fakes
{
    public sealed class FakeJwtTokenService : IJwtTokenService
    {
        public DateTimeOffset LastAccessExpiresAt { get; private set; }

        public (string Token, DateTime ExpiresAtUtc) CreateToken(
            Guid userId,
            string email,
            string name,
            UserRole role)
        {
            var expires = DateTime.UtcNow.AddMinutes(5);
            LastAccessExpiresAt = expires;
            var token = $"access-{userId:N}";
            return (token, expires);
        }

    }
}
