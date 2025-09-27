using System.Security.Claims;

namespace Application.Common.Abstractions.Security
{
    public interface IJwtTokenService
    {
        string CreateToken(Guid userId, string email, string role);
        ClaimsPrincipal? ValidateToken(string token);
    }
}
