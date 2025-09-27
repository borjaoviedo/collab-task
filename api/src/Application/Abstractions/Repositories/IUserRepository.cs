using Domain.Entities;
using Domain.Enums;

namespace Application.Abstractions.Repositories
{
    public interface IUserRepository
    {
        Task<Guid> CreateAsync(User item, CancellationToken ct = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<User?> SetRoleAsync(Guid id, UserRole role, CancellationToken ct = default);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
        Task<bool> AnyAdminAsync(CancellationToken ct = default);
        Task<int> CountAdminsAsync(CancellationToken ct = default);
    }
}
