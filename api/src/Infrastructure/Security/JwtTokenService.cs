using Application.Common.Abstractions.Security;
using Application.Common.Abstractions.Time;
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

        public JwtTokenService(IDateTimeProvider clock, JwtOptions options)
        {
            _clock = clock;
            _options = options;
        }

        public string CreateToken(Guid userId, string email, string role)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role)
        };

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: _clock.UtcNow.UtcDateTime,
                expires: _clock.UtcNow.AddMinutes(_options.ExpMinutes).UtcDateTime,
                signingCredentials: creds);

            return _handler.WriteToken(token);
        }
    }
}
