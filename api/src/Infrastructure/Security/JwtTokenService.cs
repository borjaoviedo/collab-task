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
