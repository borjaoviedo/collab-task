using Application.Abstractions.Auth;
using Application.Abstractions.Persistence;
using Application.Abstractions.Security;
using Application.Auth.DTOs;
using Application.Auth.Mapping;
using Application.Common.Exceptions;
using Application.Users.Abstractions;
using Application.Users.DTOs;
using Application.Users.Mapping;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Users.Services
{
    /// <summary>
    /// Application write-side service for <see cref="User"/> aggregates.
    /// Handles user registration, authentication, profile updates, role changes,
    /// and deletion operations. All write operations enforce domain invariants,
    /// uniqueness constraints, and optimistic concurrency through <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <param name="userRepository">
    /// Repository used for retrieving, tracking, modifying and persisting <see cref="User"/> entities.
    /// </param>
    /// <param name="unitOfWork">
    /// Coordinates transactional persistence and enforces optimistic concurrency using
    /// <see cref="DomainMutation"/> and <see cref="MutationKind"/>.
    /// </param>
    /// <param name="currentUserService">
    /// Provides information about the currently authenticated user, such as <c>UserId</c>.
    /// </param>
    /// <param name="passwordHasher">
    /// Abstraction used to securely hash and verify user passwords using a salt+hash mechanism.
    /// </param>
    /// <param name="jwtTokenService">
    /// Service responsible for issuing signed JWT access tokens after successful authentication
    /// or user registration.
    /// </param>
    public sealed class UserWriteService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService) : IUserWriteService
    {
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ICurrentUserService _currentUserService = currentUserService;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly IJwtTokenService _jwtTokenService = jwtTokenService;

        /// <inheritdoc/>
        public async Task<AuthTokenReadDto> RegisterAsync(
            UserRegisterDto dto,
            CancellationToken ct = default)
        {
            var emailVo = Email.Create(dto.Email);

            var existing = await _userRepository.GetByEmailAsync(emailVo, ct);
            if (existing is not null)
                throw new ConflictException("A user with the specified email already exists.");

            var userNameVo = UserName.Create(dto.Name);

            // Hash and salt the password using the security abstraction
            var (hash, salt) = _passwordHasher.Hash(dto.Password);

            var user = User.Create(emailVo, userNameVo, hash, salt, UserRole.User);

            await _userRepository.AddAsync(user, ct);

            // Persist the new user
            var mutation = await _unitOfWork.SaveAsync(MutationKind.Create, ct);
            if (mutation != DomainMutation.Created)
                throw new ConflictException("User could not be created due to a conflicting state.");

            // Issue access token using the JWT token service
            var (accessToken, accessExpiresAt) = _jwtTokenService.CreateToken(
                user.Id,
                user.Email.Value,
                user.Name.Value,
                user.Role);

            return user.ToAuthTokenReadDto(accessToken, accessExpiresAt);
        }

        /// <inheritdoc/>
        public async Task<AuthTokenReadDto> LoginAsync(UserLoginDto dto, CancellationToken ct = default)
        {
            var emailVo = Email.Create(dto.Email);

            var user = await _userRepository.GetByEmailAsync(emailVo, ct)
                ?? throw new InvalidCredentialsException("Invalid credentials.");

            var isValidPassword = _passwordHasher.Verify(
                dto.Password,
                user.PasswordSalt,
                user.PasswordHash);

            if (!isValidPassword)
                throw new InvalidCredentialsException("Invalid credentials.");

            var (accessToken, accessExpiresAt) = _jwtTokenService.CreateToken(
                user.Id,
                user.Email.Value,
                user.Name.Value,
                user.Role);

            return user.ToAuthTokenReadDto(accessToken, accessExpiresAt);
        }

        /// <inheritdoc/>
        public async Task<UserReadDto> RenameAsync(
            UserRenameDto dto,
            CancellationToken ct = default)
        {
            var currentUserId = (Guid)_currentUserService.UserId!;
            var user = await _userRepository.GetByIdForUpdateAsync(currentUserId, ct)
                ?? throw new NotFoundException("Current user not found.");

            var newName = UserName.Create(dto.NewName);
            user.Rename(newName);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Update, ct);
            if (mutation != DomainMutation.Updated)
                throw new ConflictException("The name could not be changed due to a conflicting state.");

            return user.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task<UserReadDto> ChangeRoleAsync(
            Guid userId,
            UserChangeRoleDto dto,
            CancellationToken ct = default)
        {
            var user = await _userRepository.GetByIdForUpdateAsync(userId, ct)
                ?? throw new NotFoundException("User not found.");

            user.ChangeRole(dto.NewRole);

            var mutation = await _unitOfWork.SaveAsync(MutationKind.Update, ct);
            if (mutation != DomainMutation.Updated)
                throw new ConflictException("The role could not be changed due to a conflicting state.");

            return user.ToReadDto();
        }

        /// <inheritdoc/>
        public async Task DeleteByIdAsync(Guid userId, CancellationToken ct = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, ct);
            if (user is null) return;

            await _userRepository.RemoveAsync(user, ct);
            var mutation = await _unitOfWork.SaveAsync(MutationKind.Delete, ct);

            if (mutation != DomainMutation.Deleted && mutation != DomainMutation.NoOp)
                throw new ConflictException("The user could not be deleted due to a conflicting state.");
        }
    }
}
