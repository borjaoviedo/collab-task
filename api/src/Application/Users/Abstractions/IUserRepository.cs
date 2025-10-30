using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Users.Abstractions
{
    /// <summary>
    /// Defines persistence operations for user entities.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>Lists all users in the persistence store.</summary>
        Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default);

        /// <summary>Retrieves a user by their email address.</summary>
        Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default);

        /// <summary>Gets a user by their identifier without tracking.</summary>
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>Gets a user by their identifier with change tracking enabled.</summary>
        Task<User?> GetTrackedByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>Adds a new user to the persistence context.</summary>
        Task AddAsync(User item, CancellationToken ct = default);

        /// <summary>Renames an existing user enforcing concurrency via row version.</summary>
        Task<PrecheckStatus> RenameAsync(
            Guid id,
            UserName newName,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Changes the role of an existing user enforcing concurrency.</summary>
        Task<PrecheckStatus> ChangeRoleAsync(
            Guid id,
            UserRole newRole,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Deletes a user if concurrency and constraints allow it.</summary>
        Task<PrecheckStatus> DeleteAsync(
            Guid id,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>
        /// Checks whether a user already exists with the specified email,
        /// optionally excluding one user ID from the comparison.
        /// </summary>
        Task<bool> ExistsWithEmailAsync(
            Email email,
            Guid? excludeUserId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Checks whether a user already exists with the specified name,
        /// optionally excluding one user ID from the comparison.
        /// </summary>
        Task<bool> ExistsWithNameAsync(
            UserName name,
            Guid? excludeUserId = null,
            CancellationToken ct = default);
    }

}
