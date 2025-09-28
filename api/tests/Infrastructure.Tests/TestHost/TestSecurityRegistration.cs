using Application.Common.Abstractions.Security;
using Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Infrastructure.Tests.TestHost
{
    public static class TestSecurityRegistration
    {
        public static IServiceCollection AddInfrastructureSecurityForTests(this IServiceCollection services)
        {
            var opts = new JwtOptions
            {
                Issuer = "ct.test",
                Audience = "ct.test.clients",
                Key = "dev-test-secret-32-bytes-minimum!!!!!",
                ExpMinutes = 30
            };
            services.AddSingleton(opts);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.Key));
            services.AddSingleton<SecurityKey>(key);
            services.AddSingleton(new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            //services.AddSingleton<IDateTimeProvider, SystemClock>();
            services.AddSingleton<IJwtTokenService, JwtTokenService>();
            services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

            return services;
        }
    }
}
