using Domain.Entities;
using Domain.Enums;

namespace Application.Users.Abstractions
{
    public interface IUserRepository
    {
        Task AddAsync(User item, CancellationToken ct = default);
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<User?> GetByNameAsync(string name, CancellationToken ct = default);
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<DomainMutation> RenameAsync(Guid id, string newName, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> ChangeRoleAsync(Guid id, UserRole role, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default);
        Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);
        Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
        Task<bool> AnyAdminAsync(CancellationToken ct = default);
        Task<int> CountAdminsAsync(CancellationToken ct = default);
    }
}
