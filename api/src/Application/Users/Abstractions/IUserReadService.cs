using Domain.Entities;

namespace Application.Users.Abstractions
{
    public interface IUserReadService
    {
        Task<User?> GetAsync(Guid userId, CancellationToken ct = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default);
    }
}
