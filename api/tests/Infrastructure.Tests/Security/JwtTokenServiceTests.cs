using Application.Common.Abstractions.Security;
using Application.Common.Abstractions.Time;
using FluentAssertions;
using Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Infrastructure.Tests.Security
{
    public sealed class FakeClock : IDateTimeProvider
    {
        private DateTimeOffset _now;
        public FakeClock(DateTimeOffset now) => _now = now;
        public DateTimeOffset UtcNow => _now;
        public void Advance(TimeSpan dt) => _now = _now.Add(dt);
    }

    public sealed class JwtTokenServiceTests
    {
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

        [Fact]
        public void CreateToken_Returns_Token_And_ExpiresAtUtc_As_Configured()
        {
            var clock = new FakeClock(DateTimeOffset.Parse("2020-01-01T00:00:00Z"));
            using var sp = BuildProvider(clock);
            var svc = sp.GetRequiredService<IJwtTokenService>();

            var (token, expiresAtUtc) = svc.CreateToken(Guid.NewGuid(), "user@demo.com", "User Name", "User");

            token.Should().NotBeNullOrWhiteSpace();
            expiresAtUtc.Kind.Should().Be(DateTimeKind.Utc);

            var expected = clock.UtcNow.AddMinutes(60).UtcDateTime;
            expiresAtUtc.Should().BeCloseTo(expected, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void CreateToken_Claims_Are_Present_And_Correct()
        {
            var clock = new FakeClock(DateTimeOffset.UnixEpoch);
            using var sp = BuildProvider(clock);
            var svc = sp.GetRequiredService<IJwtTokenService>();

            var userId = Guid.NewGuid();
            var (token, _) = svc.CreateToken(userId, "claims@demo.com", "User Name", "Admin");

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            jwt.Issuer.Should().Be("Test");
            jwt.Audiences.Should().Contain("Test");

            jwt.Claims.Select(c => c.Type).Should().Contain(new[]
            {
                JwtRegisteredClaimNames.Sub,
                JwtRegisteredClaimNames.Email,
                JwtRegisteredClaimNames.Jti,
                ClaimTypes.Name,
                ClaimTypes.Role
            });

            jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(userId.ToString());
            jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be("claims@demo.com");
            jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value.Should().Be("User Name");
            jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value.Should().Be("Admin");

            // nbf == now, exp == now + 60m (approx)
            jwt.ValidFrom.Should().Be(clock.UtcNow.UtcDateTime);
            jwt.ValidTo.Should().BeCloseTo(clock.UtcNow.AddMinutes(60).UtcDateTime, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void CreateToken_Exp_Matches_Returned_ExpiresAtUtc()
        {
            var clock = new FakeClock(DateTimeOffset.Parse("2021-05-10T12:00:00Z"));
            using var sp = BuildProvider(clock);
            var svc = sp.GetRequiredService<IJwtTokenService>();

            var (token, expiresAtUtc) = svc.CreateToken(Guid.NewGuid(), "exp@demo.com", "Exp Name" , "User");
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            jwt.ValidTo.Should().BeCloseTo(expiresAtUtc, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Token_Expiration_Behaves_Over_Time()
        {
            var clock = new FakeClock(DateTimeOffset.Parse("2020-01-01T00:00:00Z"));
            using var sp = BuildProvider(clock);
            var svc = sp.GetRequiredService<IJwtTokenService>();

            var (token, _) = svc.CreateToken(Guid.NewGuid(), "t@demo.com", "User T Name", "User");
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            var exp = jwt.ValidTo; // UTC
            exp.Should().BeAfter(clock.UtcNow.UtcDateTime);

            clock.Advance(TimeSpan.FromDays(2));
            exp.Should().BeBefore(clock.UtcNow.UtcDateTime);
        }

        [Fact]
        public void CreateToken_Generates_Unique_Jti_Per_Call()
        {
            var clock = new FakeClock(DateTimeOffset.UnixEpoch);
            using var sp = BuildProvider(clock);
            var svc = sp.GetRequiredService<IJwtTokenService>();

            var (t1, _) = svc.CreateToken(Guid.NewGuid(), "a@demo.com", "User A", "User");
            var (t2, _) = svc.CreateToken(Guid.NewGuid(), "b@demo.com", "User B", "User");

            var j1 = new JwtSecurityTokenHandler().ReadJwtToken(t1).Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
            var j2 = new JwtSecurityTokenHandler().ReadJwtToken(t2).Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

            j1.Should().NotBeNullOrWhiteSpace();
            j2.Should().NotBeNullOrWhiteSpace();
            j1.Should().NotBe(j2);
        }
    }
}
