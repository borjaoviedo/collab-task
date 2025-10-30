using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Users.Abstractions
{
    /// <summary>
    /// Provides read-only access to user entities.
    /// </summary>
    public interface IUserReadService
    {
        /// <summary>Retrieves a user by their unique identifier.</summary>
        Task<User?> GetAsync(Guid userId, CancellationToken ct = default);

        /// <summary>Retrieves a user by their email address.</summary>
        Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default);

        /// <summary>Lists all users in the system.</summary>
        Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default);
    }

}
