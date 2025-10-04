namespace Application.Common.Abstractions.Auth
{
    public interface ICurrentUserService
    {
        bool IsAuthenticated { get; }
        Guid? UserId { get; }
        string? Email { get; }
        string? Name { get; }
        string? Role { get; }
    }
}
