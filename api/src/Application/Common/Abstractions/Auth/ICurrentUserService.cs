
namespace Application.Common.Abstractions.Auth
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
    }
}
