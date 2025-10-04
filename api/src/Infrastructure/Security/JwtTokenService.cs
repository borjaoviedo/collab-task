using Application.Common.Abstractions.Security;
using Application.Common.Abstractions.Time;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Security
{
    public sealed class JwtTokenService : IJwtTokenService
    {
        private readonly IDateTimeProvider _clock;
        private readonly JwtOptions _options;
        private readonly JwtSecurityTokenHandler _handler = new();

        public JwtTokenService(IDateTimeProvider clock, IOptions<JwtOptions> options)
        {
            _clock = clock;
            _options = options.Value;
        }

        public (string Token, DateTime ExpiresAtUtc) CreateToken(Guid userId, string email, string name, string role)
        {
            var key = GetSigningKey(_options.Key);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new(ClaimTypes.Name, name),
                new(ClaimTypes.Role, role),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var now = _clock.UtcNow;
            var expiresAtUtc = now.AddMinutes(_options.ExpMinutes).UtcDateTime;

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: expiresAtUtc,
                signingCredentials: creds);

            var tokenStr = _handler.WriteToken(token);

            return (tokenStr, expiresAtUtc);
        }

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
