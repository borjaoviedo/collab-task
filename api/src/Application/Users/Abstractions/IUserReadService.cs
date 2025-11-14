using Application.Users.DTOs;

namespace Application.Users.Abstractions
{
    /// <summary>
    /// Provides read-only access to user entities.
    /// </summary>
    public interface IUserReadService
    {
        /// <summary>
        /// Retrieves a user by its unique identifier.
        /// Throws <see cref="Common.Exceptions.NotFoundException"/> when the user does not exist.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to retrieve.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A <see cref="UserReadDto"/> representing the user.</returns>
        Task<UserReadDto> GetByIdAsync(Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Retrieves the profile of the currently authenticated user.
        /// Throws <see cref="UnauthorizedAccessException"/> when no user is authenticated,
        /// or <see cref="Common.Exceptions.NotFoundException"/> when the current user cannot be found.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A <see cref="UserReadDto"/> describing the current user.</returns>
        Task<UserReadDto> GetCurrentAsync(CancellationToken ct = default);

        /// <summary>
        /// Searches for users.
        /// </summary>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// A read-only list of <see cref="UserReadDto"/> objects.
        /// </returns>
        Task<IReadOnlyList<UserReadDto>> ListAsync(CancellationToken ct = default);
    }
}
