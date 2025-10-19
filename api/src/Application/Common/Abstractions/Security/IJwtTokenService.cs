using Domain.Enums;
using System.Security.Claims;

namespace Application.Common.Abstractions.Security
{
    public interface IJwtTokenService
    {
        (string Token, DateTime ExpiresAtUtc) CreateToken(Guid userId, string email, string name, UserRole role);
        ClaimsPrincipal? ValidateToken(string token);
    }
}
