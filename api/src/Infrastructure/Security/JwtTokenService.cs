using Application.Abstractions.Security;
using Application.Abstractions.Time;
using Domain.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Security
{
    /// <summary>
    /// Issues and validates JSON Web Tokens using symmetric signing credentials.
    /// Relies on <see cref="IDateTimeProvider"/> for deterministic time and <see cref="JwtOptions"/> for issuer, audience, key, and expiration.
    /// </summary>
    public sealed class JwtTokenService(IDateTimeProvider clock, IOptions<JwtOptions> options) : IJwtTokenService
    {
        private readonly IDateTimeProvider _clock = clock;
        private readonly JwtOptions _options = options.Value;
        private readonly JwtSecurityTokenHandler _handler = new();

        /// <summary>
        /// Creates a signed JWT with standard claims and an absolute expiration.
        /// </summary>
        /// <param name="userId">User unique identifier mapped to the <c>sub</c> claim.</param>
        /// <param name="email">User email mapped to <c>email</c>.</param>
        /// <param name="name">User display name mapped to <see cref="ClaimTypes.Name"/>.</param>
        /// <param name="role">User role mapped to <see cref="ClaimTypes.Role"/>.</param>
        /// <returns>A tuple with the compact token string and its UTC expiration instant.</returns>
        public (string Token, DateTime ExpiresAtUtc) CreateToken(
            Guid userId,
            string email,
            string name,
            UserRole role)
        {
            var key = GetSigningKey(_options.Key);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new(ClaimTypes.Name, name),
                new(ClaimTypes.Role, role.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var now = _clock.UtcNow;
            var expiresAtUtc = now.AddMinutes(_options.ExpMinutes).UtcDateTime;

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims,
                notBefore: now.UtcDateTime,
                expiresAtUtc,
                signingCredentials: creds);

            var tokenStr = _handler.WriteToken(token);

            return (tokenStr, expiresAtUtc);
        }

        /// <summary>
        /// Validates a JWT and returns the claims principal if valid, otherwise <c>null</c>.
        /// </summary>
        /// <param name="token">Compact serialized JWT.</param>
        /// <returns><see cref="ClaimsPrincipal"/> when valid; otherwise <c>null</c>.</returns>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            var parameters = BuildValidationParameters(_options);

            try
            {
                var principal = _handler.ValidateToken(token, parameters, out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Builds strict validation parameters from configured options.
        /// </summary>
        /// <param name="opts">JWT configuration.</param>
        /// <returns>Validation parameters for issuer, audience, key, and lifetime.</returns>
        private static TokenValidationParameters BuildValidationParameters(JwtOptions opts)
        {
            return new TokenValidationParameters
            {
                ValidateIssuer = !string.IsNullOrWhiteSpace(opts.Issuer),
                ValidIssuer = opts.Issuer,
                ValidateAudience = !string.IsNullOrWhiteSpace(opts.Audience),
                ValidAudience = opts.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = GetSigningKey(opts.Key),
                RequireExpirationTime = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };
        }

        /// <summary>
        /// Creates a symmetric signing key from a UTF-8 secret.
        /// </summary>
        /// <param name="key">Secret configured in <c>Jwt:Key</c>.</param>
        /// <returns>A <see cref="SymmetricSecurityKey"/> instance.</returns>
        private static SymmetricSecurityKey GetSigningKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Jwt:Key is missing.");

            var bytes = Encoding.UTF8.GetBytes(key);
            if (bytes.Length < 32)
                throw new InvalidOperationException("Jwt:Key must be at least 32 bytes.");

            return new SymmetricSecurityKey(bytes);
        }
    }
}
