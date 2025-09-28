using Application.Common.Abstractions.Security;
using Application.Common.Abstractions.Time;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Security;
using Infrastructure.Tests.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
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
        public void CreateToken_ContainsExpectedRegisteredAndCustomClaims()
        {
            var clock = new FakeClock(DateTimeOffset.UnixEpoch);
            using var sp = BuildProvider(clock);
            var svc = sp.GetRequiredService<IJwtTokenService>();

            var user = new User { Id = Guid.NewGuid(), Email = Email.Create("claims@demo.com"), Role = UserRole.Admin };
            var token = svc.CreateToken(user.Id, user.Email.Value, "Admin");

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            jwt.Claims.Select(c => c.Type).Should().Contain([JwtRegisteredClaimNames.Sub, JwtRegisteredClaimNames.Email, ClaimTypes.Role]);
            jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be("claims@demo.com");
            jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value.Should().Be("Admin");
        }

        [Fact]
        public void Token_Expires_As_Configured()
        {
            var clock = new FakeClock(DateTimeOffset.Parse("2020-01-01T00:00:00Z"));
            using var sp = BuildProvider(clock);
            var svc = sp.GetRequiredService<IJwtTokenService>();

            var user = new User { Id = Guid.NewGuid(), Email = Email.Create("exp@demo.com"), Role = UserRole.User };
            var token = svc.CreateToken(user.Id, user.Email.Value, "User");

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            var exp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Exp).Value));
            exp.Should().BeAfter(clock.UtcNow);

            clock.Advance(TimeSpan.FromDays(2));
            exp.Should().BeBefore(clock.UtcNow);
        }
    }
}
