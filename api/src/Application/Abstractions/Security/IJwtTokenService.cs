
namespace Application.Abstractions.Security
{
    public interface IJwtTokenService
    {
        string CreateAccessToken(Guid userId, string email, string role, DateTimeOffset nowUtc);
        (bool IsValid, Guid UserId, string Email, string Role)? Validate(string token);
    }
}
