using Application.Users.Abstractions;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Users.Services
{
    /// <summary>
    /// Read-only application service for users.
    /// </summary>
    public sealed class UserReadService(IUserRepository repo) : IUserReadService
    {
        /// <summary>Retrieves a user by their identifier.</summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<User?> GetAsync(Guid userId, CancellationToken ct = default)
            => await repo.GetByIdAsync(userId, ct);

        /// <summary>Retrieves a user by email.</summary>
        /// <param name="email">The email value object.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default)
            => await repo.GetByEmailAsync(email, ct);

        /// <summary>Lists all users.</summary>
        /// <param name="ct">Cancellation token.</param>
        public async Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default)
            => await repo.ListAsync(ct);
    }

}
