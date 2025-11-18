using Application.Auth.DTOs;
using Application.Users.DTOs;

namespace Application.Users.Abstractions
{
    /// <summary>
    /// Handles creation and mutation commands for user entities at the application level.
    /// </summary>
    public interface IUserWriteService
    {
        /// <summary>
        /// Registers a new user account and issues an initial access and refresh token pair.
        /// </summary>
        /// <param name="dto">The registration data containing the user's email, name, and password.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// An <see cref="AuthTokenReadDto"/> containing the issued access and refresh tokens,
        /// along with the new user's identity information.
        /// </returns>
        Task<AuthTokenReadDto> RegisterAsync(UserRegisterDto dto, CancellationToken ct = default);

        /// <summary>
        /// Authenticates an existing user using their credentials
        /// and issues a new access and refresh token pair.
        /// </summary>
        /// <param name="dto">The login credentials containing the user's email and password.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>
        /// An <see cref="AuthTokenReadDto"/> with the new access and refresh tokens
        /// if authentication succeeds.
        /// </returns>
        Task<AuthTokenReadDto> LoginAsync(UserLoginDto dto, CancellationToken ct = default);

        /// <summary>
        /// Changes the display name of the currently authenticated user.
        /// </summary>
        /// <param name="dto">The request body containing the new display name.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>The updated <see cref="UserReadDto"/> reflecting the name change.</returns>
        Task<UserReadDto> RenameAsync(UserRenameDto dto, CancellationToken ct = default);

        /// <summary>
        /// Changes the role of the specified user (administrative operation).
        /// </summary>
        /// <param name="userId">The identifier of the user whose role will be updated.</param>
        /// <param name="dto">The request body containing the new role value.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        /// <returns>The updated <see cref="UserReadDto"/> after the role change.</returns>
        Task<UserReadDto> ChangeRoleAsync(Guid userId, UserChangeRoleDto dto, CancellationToken ct = default);

        /// <summary>
        /// Deletes a user account by its unique identifier (administrative operation).
        /// </summary>
        /// <param name="userId">The identifier of the user to delete.</param>
        /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
        Task DeleteByIdAsync(Guid userId, CancellationToken ct = default);
    }
}
