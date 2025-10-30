using Application.Common.Abstractions.Extensions;
using Application.Common.Abstractions.Persistence;
using Application.Users.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Users.Services
{
    /// <summary>
    /// Write-side application service for users.
    /// </summary>
    public sealed class UserWriteService(IUserRepository repo, IUnitOfWork uow) : IUserWriteService
    {
        /// <summary>
        /// Creates a new user after validating uniqueness by email and name.
        /// </summary>
        /// <param name="email">Email value object.</param>
        /// <param name="name">User name value object.</param>
        /// <param name="hash">Password hash bytes.</param>
        /// <param name="salt">Password salt bytes.</param>
        /// <param name="role">Initial role.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The mutation result and the created user when successful.</returns>
        public async Task<(DomainMutation, User?)> CreateAsync(
            Email email,
            UserName name,
            byte[] hash,
            byte[] salt,
            UserRole role,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                return (DomainMutation.NoOp, null);

            var existsWithEmail = await repo.ExistsWithEmailAsync(email, excludeUserId: null, ct);
            var existsWithName = await repo.ExistsWithNameAsync(name, excludeUserId: null, ct);
            var exists = existsWithEmail || existsWithName;

            if (exists) return (DomainMutation.Conflict, null);

            var user = User.Create(email, name, hash, salt, role);
            await repo.AddAsync(user, ct);

            var createResult = await uow.SaveAsync(MutationKind.Create, ct);
            return (createResult, user);
        }

        /// <summary>
        /// Renames an existing user with concurrency enforcement.
        /// </summary>
        /// <param name="id">User identifier.</param>
        /// <param name="newName">New user name.</param>
        /// <param name="rowVersion">Concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<DomainMutation> RenameAsync(
            Guid id,
            UserName newName,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var rename = await repo.RenameAsync(id, newName, rowVersion, ct);
            if (rename != PrecheckStatus.Ready) return rename.ToErrorDomainMutation();

            var updateResult = await uow.SaveAsync(MutationKind.Update, ct);
            return updateResult;
        }

        /// <summary>
        /// Changes the role of an existing user with concurrency enforcement.
        /// </summary>
        /// <param name="id">User identifier.</param>
        /// <param name="newRole">New role.</param>
        /// <param name="rowVersion">Concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<DomainMutation> ChangeRoleAsync(
            Guid id,
            UserRole newRole,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var changeRole = await repo.ChangeRoleAsync(id, newRole, rowVersion, ct);
            if (changeRole != PrecheckStatus.Ready) return changeRole.ToErrorDomainMutation();

            var updateResult = await uow.SaveAsync(MutationKind.Update, ct);
            return updateResult;
        }

        /// <summary>
        /// Deletes a user with concurrency enforcement.
        /// </summary>
        /// <param name="id">User identifier.</param>
        /// <param name="rowVersion">Concurrency token.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<DomainMutation> DeleteAsync(
            Guid id,
            byte[] rowVersion,
            CancellationToken ct = default)
        {
            var delete = await repo.DeleteAsync(id, rowVersion, ct);
            if (delete != PrecheckStatus.Ready) return delete.ToErrorDomainMutation();

            var deleteResult = await uow.SaveAsync(MutationKind.Delete, ct);
            return deleteResult;
        }
    }

}
