using Domain.Entities;
using Domain.Enums;

namespace Application.Users.Abstractions
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<User?> GetByNameAsync(string name, CancellationToken ct = default);
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);

        Task AddAsync(User item, CancellationToken ct = default);
        Task<DomainMutation> RenameAsync(Guid id, string newName, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> ChangeRoleAsync(Guid id, UserRole role, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default);

        Task<bool> ExistsWithEmailAsync(string email, Guid? excludeUserId = null, CancellationToken ct = default);
        Task<bool> ExistsWithNameAsync(string name, Guid? excludeUserId = null, CancellationToken ct = default);
        Task<bool> AnyAdminAsync(CancellationToken ct = default);
        Task<int> CountAdminsAsync(CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
