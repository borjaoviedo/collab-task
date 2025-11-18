using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Users.Abstractions
{
    /// <summary>
    /// Defines persistence operations for user entities.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Retrieves all users from the persistence store.
        /// </summary>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>A read-only list of <see cref="User"/> entities.</returns>
        Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default);

        /// <summary>
        /// Retrieves a user by its unique identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to retrieve.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>The <see cref="User"/> entity, or <c>null</c> if no user is found.</returns>
        Task<User?> GetByIdAsync(Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Retrieves a <see cref="User"/> aggregate for update with EF Core tracking enabled.
        /// Use this when you plan to mutate the aggregate so EF can detect changed columns
        /// and persist minimal updates without calling <c>Update(entity)</c>.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to retrieve.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>The tracked <see cref="User"/> entity, or <c>null</c> if no user is found.</returns>
        Task<User?> GetByIdForUpdateAsync(Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Retrieves a user by their email address.
        /// </summary>
        /// <param name="email">The email address of the user to retrieve.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>The <see cref="User"/> entity, or <c>null</c> if no user is found.</returns>
        Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default);

        /// <summary>
        /// Adds a new user entity to the persistence context.
        /// </summary>
        /// <param name="user">The user entity to add.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task AddAsync(User user, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing user entity within the persistence context.
        /// </summary>
        /// <param name="user">The user entity with modified state.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task UpdateAsync(User user, CancellationToken ct = default);

        /// <summary>
        /// Removes a user entity from the persistence context.
        /// </summary>
        /// <param name="user">The user entity to remove.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task RemoveAsync(User user, CancellationToken ct = default);
    }
}
