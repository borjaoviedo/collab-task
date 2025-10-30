using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Users.Abstractions
{
    /// <summary>
    /// Handles creation and mutation commands for user entities at the application level.
    /// </summary>
    public interface IUserWriteService
    {
        /// <summary>Creates a new user with the given credentials and role.</summary>
        Task<(DomainMutation, User?)> CreateAsync(
            Email email,
            UserName name,
            byte[] hash,
            byte[] salt,
            UserRole role,
            CancellationToken ct = default);

        /// <summary>Renames an existing user.</summary>
        Task<DomainMutation> RenameAsync(
            Guid id,
            UserName newName,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Changes the role of an existing user.</summary>
        Task<DomainMutation> ChangeRoleAsync(
            Guid id,
            UserRole newRole,
            byte[] rowVersion,
            CancellationToken ct = default);

        /// <summary>Deletes an existing user.</summary>
        Task<DomainMutation> DeleteAsync(
            Guid id,
            byte[] rowVersion,
            CancellationToken ct = default);
    }

}
