using Domain.Enums;

namespace Application.Abstractions.Security
{
    /// <summary>
    /// Issues and validates JSON Web Tokens for API authentication.
    /// </summary>
    public interface IJwtTokenService
    {
        /// <summary>
        /// Creates a signed JWT containing standard and custom claims for the specified user.
        /// </summary>
        /// <param name="userId">User identifier to embed as a subject claim.</param>
        /// <param name="email">User email to include as a claim.</param>
        /// <param name="name">User display name to include as a claim.</param>
        /// <param name="role">User role to include as an authorization claim.</param>
        /// <returns>
        /// The serialized token and its UTC expiration instant.
        /// </returns>
        (string Token, DateTime ExpiresAtUtc) CreateToken(
            Guid userId,
            string email,
            string name,
            UserRole role);
    }
}
