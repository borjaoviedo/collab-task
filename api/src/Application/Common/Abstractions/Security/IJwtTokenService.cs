namespace Application.Common.Abstractions.Security
{
    public interface IJwtTokenService
    {
        string CreateToken(Guid userId, string email, string role);
    }
}
