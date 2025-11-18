using Application.Abstractions.Auth;
using Application.Common.Exceptions;
using Application.Users.Abstractions;
using Application.Users.DTOs;
using Application.Users.Mapping;
using Domain.Entities;

namespace Application.Users.Services
{
    /// <summary>
    /// Provides read-only user query operations for the application layer.
    /// Supports retrieval of users by identifier, resolution of the current authenticated user,
    /// and searches across all registered users.
    /// </summary>
    /// <param name="userRepository">
    /// Repository abstraction used to query <see cref="User"/> aggregates from the persistence layer.
    /// </param>
    /// <param name="currentUserService">
    /// Service abstraction that exposes information about the currently authenticated user.
    /// </param>
    public sealed class UserReadService(
        IUserRepository userRepository,
        ICurrentUserService currentUserService) : IUserReadService
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly ICurrentUserService _currentUserService = currentUserService;

        /// <inheritdoc/>
        public async Task<UserReadDto> GetByIdAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, ct)
                // 404 if the user does not exist
                ?? throw new NotFoundException("User not found.");

            return user.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<UserReadDto> GetCurrentAsync(CancellationToken ct = default)
        {
            var currentUserId = (Guid)_currentUserService.UserId!;

            var user = await _userRepository.GetByIdAsync(currentUserId, ct)
                // 404 if the current user record is missing
                ?? throw new NotFoundException("Current user not found.");

            return user.ToReadDto();
        }

        /// <summary>Lists all users.</summary>
        /// <param name="ct">Cancellation token.</param>
        public async Task<IReadOnlyList<UserReadDto>> ListAsync(CancellationToken ct = default)
        {
            var users = await _userRepository.ListAsync(ct);

            // Project domain entities to DTOs for API consumption
            return users
                .Select(u => u.ToReadDto())
                .ToList();
        }
    }
}
