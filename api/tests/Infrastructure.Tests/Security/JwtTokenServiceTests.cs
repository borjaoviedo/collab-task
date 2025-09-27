using Application.Common.Abstractions.Security;
using Application.Common.Abstractions.Time;
using FluentAssertions;
using Infrastructure.Security;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Infrastructure.Tests.Security
{
    public class JwtTokenServiceTests
    {
        private readonly Mock<IDateTimeProvider> _clock = new();
        private readonly JwtOptions _opts;
        private readonly IJwtTokenService _sut;

        public JwtTokenServiceTests()
        {
            _clock.Setup(c => c.UtcNow).Returns(new DateTimeOffset(2025, 9, 27, 10, 0, 0, TimeSpan.Zero));

            _opts = new JwtOptions
            {
                Issuer = "collabtask",
                Audience = "collabtask-api",
                SigningKey = "dev-signing-key-change-me-very-long-256bit",
                ExpMinutes = 30
            };

            _sut = new JwtTokenService(_clock.Object, _opts);
        }

        [Fact]
        public void CreateToken_ContainsExpectedRegisteredAndCustomClaims()
        {
            var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var token = _sut.CreateToken(userId, "user@demo.com", "Admin");

            token.Should().NotBeNullOrWhiteSpace();

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            jwt.Issuer.Should().Be(_opts.Issuer);
            jwt.Audiences.Should().Contain(_opts.Audience);

            jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId.ToString());
            jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "user@demo.com");
            jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");

            var exp = jwt.ValidTo; // UTC
            exp.Should().Be(new DateTime(2025, 9, 27, 10, 30, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void ValidateToken_ReturnsPrincipal_ForValidToken()
        {
            var userId = Guid.NewGuid();
            var token = _sut.CreateToken(userId, "user@demo.com", "User");

            var principal = _sut.ValidateToken(token);

            principal.Should().NotBeNull();
            principal!.FindFirst(ClaimTypes.Email)?.Value.Should().Be("user@demo.com");
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(userId.ToString());
            principal.IsInRole("User").Should().BeTrue();
        }

        [Fact]
        public void CreateToken_RespectsClock_ForExpiration()
        {
            _clock.Setup(c => c.UtcNow).Returns(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));

            var token = _sut.CreateToken(Guid.NewGuid(), "a@b.com", "User");

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            jwt.ValidTo.Should().Be(new DateTime(2030, 1, 1, 0, 30, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void Token_WithWrongIssuer_IsDetectable()
        {
            var badOpts = _opts with { Issuer = "other" };
            var badSut = new JwtTokenService(_clock.Object, badOpts);

            var token = badSut.CreateToken(Guid.NewGuid(), "x@y.com", "User");
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            jwt.Issuer.Should().Be("other");
            jwt.Issuer.Should().NotBe(_opts.Issuer);
        }
    }
}
