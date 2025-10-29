using Application.Common.Abstractions.Security;
using Application.Common.Abstractions.Time;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TestHelpers.Common.Time;

namespace Infrastructure.Tests.Security
{
    public sealed class JwtTokenServiceTests
    {
        private readonly FakeClock _clock = new(
            DateTimeOffset.Parse("2025-10-29T12:00:00Z",
                CultureInfo.InvariantCulture));

        [Fact]
        public void CreateToken_Returns_Token_And_ExpiresAtUtc_As_Configured()
        {
            using var sp = BuildProvider(_clock);
            var jwtSvc = sp.GetRequiredService<IJwtTokenService>();

            var (token, expiresAtUtc) = jwtSvc.CreateToken(
                userId: Guid.NewGuid(),
                "user@demo.com",
                "User Name",
                UserRole.User);

            token.Should().NotBeNullOrWhiteSpace();
            expiresAtUtc.Kind.Should().Be(DateTimeKind.Utc);

            var expected = _clock.UtcNow.AddMinutes(60).UtcDateTime;
            expiresAtUtc.Should().BeCloseTo(expected, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void CreateToken_Claims_Are_Present_And_Correct()
        {
            var clock = new FakeClock(DateTimeOffset.UnixEpoch);
            using var sp = BuildProvider(clock);
            var jwtSvc = sp.GetRequiredService<IJwtTokenService>();

            var userId = Guid.NewGuid();
            var (token, _) = jwtSvc.CreateToken(
                userId,
                "claims@demo.com",
                "User Name",
                UserRole.Admin);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            jwt.Issuer.Should().Be("Test");
            jwt.Audiences.Should().Contain("Test");

            jwt.Claims.Select(c => c.Type).Should().Contain(
                [
                JwtRegisteredClaimNames.Sub,
                JwtRegisteredClaimNames.Email,
                JwtRegisteredClaimNames.Jti,
                ClaimTypes.Name,
                ClaimTypes.Role
                ]);

            jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(userId.ToString());
            jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be("claims@demo.com");
            jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value.Should().Be("User Name");
            jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value.Should().Be("Admin");

            // nbf == now, exp == now + 60m (approx)
            jwt.ValidFrom.Should().Be(clock.UtcNow.UtcDateTime);
            jwt.ValidTo.Should().BeCloseTo(
                clock.UtcNow.AddMinutes(60).UtcDateTime,
                TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void CreateToken_Exp_Matches_Returned_ExpiresAtUtc()
        {
            using var sp = BuildProvider(_clock);
            var jwtSvc = sp.GetRequiredService<IJwtTokenService>();

            var (token, expiresAtUtc) = jwtSvc.CreateToken(
                userId: Guid.NewGuid(),
                "exp@demo.com",
                "Exp Name" ,
                UserRole.User);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            jwt.ValidTo.Should().BeCloseTo(expiresAtUtc, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Token_Expiration_Behaves_Over_Time()
        {
            using var sp = BuildProvider(_clock);
            var jwtSvc = sp.GetRequiredService<IJwtTokenService>();

            var (token, _) = jwtSvc.CreateToken(
                userId: Guid.NewGuid(),
                "t@demo.com",
                "User T Name",
                UserRole.User);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            var expiresAt = jwt.ValidTo;
            expiresAt.Should().BeAfter(_clock.UtcNow.UtcDateTime);

            var inTwoDays = _clock.UtcNow.AddDays(2);
            expiresAt.Should().BeBefore(inTwoDays.UtcDateTime);
        }

        [Fact]
        public void CreateToken_Generates_Unique_Jti_Per_Call()
        {
            var clock = new FakeClock(DateTimeOffset.UnixEpoch);
            using var sp = BuildProvider(clock);
            var jwtSvc = sp.GetRequiredService<IJwtTokenService>();

            var (token1, _) = jwtSvc.CreateToken(
                userId: Guid.NewGuid(),
                "a@demo.com",
                "User A",
                UserRole.User);
            var (token2, _) = jwtSvc.CreateToken(
                userId: Guid.NewGuid(),
                "b@demo.com",
                "User B",
                UserRole.User);

            var j1 = new JwtSecurityTokenHandler().ReadJwtToken(token1).Claims
                .First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
            var j2 = new JwtSecurityTokenHandler().ReadJwtToken(token2).Claims
                .First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

            j1.Should().NotBeNullOrWhiteSpace();
            j2.Should().NotBeNullOrWhiteSpace();
            j1.Should().NotBe(j2);
        }

        // -------- HELPER --------

        private static ServiceProvider BuildProvider(IDateTimeProvider clock)
        {
            var services = new ServiceCollection();

            services.AddSingleton(clock);
            services.AddSingleton(Options.Create(new JwtOptions
            {
                Issuer = "Test",
                Audience = "Test",
                Key = new string('k', 32),
                ExpMinutes = 60
            }));

            services.AddScoped<IJwtTokenService, JwtTokenService>();

            return services.BuildServiceProvider();
        }
    }
}
