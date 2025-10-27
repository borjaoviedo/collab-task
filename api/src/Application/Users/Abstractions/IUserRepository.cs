using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Users.Abstractions
{
    public interface IUserRepository
    {
        Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default);
        Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default);
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default);

        Task AddAsync(User item, CancellationToken ct = default);

        Task<PrecheckStatus> RenameAsync(
            Guid id,
            UserName newName,
            byte[] rowVersion,
            CancellationToken ct = default);
        Task<PrecheckStatus> ChangeRoleAsync(
            Guid id,
            UserRole newRole,
            byte[] rowVersion,
            CancellationToken ct = default);
        Task<PrecheckStatus> DeleteAsync(
            Guid id,
            byte[] rowVersion,
            CancellationToken ct = default);

        Task<bool> ExistsWithEmailAsync(
            Email email,
            Guid? excludeUserId = null,
            CancellationToken ct = default);
        Task<bool> ExistsWithNameAsync(
            UserName name,
            Guid? excludeUserId = null,
            CancellationToken ct = default);
    }
}
