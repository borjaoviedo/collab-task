using Domain.Enums;
using System.Security.Claims;

namespace Application.Abstractions.Security
{
    /// <summary>
    /// Issues and validates JSON Web Tokens for API authentication.
    /// </summary>
    /// <remarks>
    /// Tokens must be signed and time-bound. Validation should verify signature, issuer/audience,
    /// expiry, and not-before constraints. No data store access is required for validation.
    /// </remarks>
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

        /// <summary>
        /// Validates a serialized JWT and returns the associated principal, or <c>null</c> if invalid or expired.
        /// </summary>
        /// <param name="token">The serialized JWT to validate.</param>
        /// <returns>
        /// A <see cref="ClaimsPrincipal"/> constructed from the token claims when valid; otherwise <c>null</c>.
        /// </returns>
        ClaimsPrincipal? ValidateToken(string token);
    }
}
