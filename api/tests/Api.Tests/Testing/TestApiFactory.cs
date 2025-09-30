using Api.Tests.Fakes;
using Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Application.Common.Abstractions.Persistence;

namespace Api.Tests.Testing
{
    public sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "Test",
                    ["Jwt:Audience"] = "Test",
                    ["Jwt:Key"] = new string('k', 32),
                    ["Jwt:ExpMinutes"] = "60"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<>));
                services.RemoveAll(typeof(DbContext));

                services.RemoveAll(typeof(Application.Users.Abstractions.IUserRepository));
                services.AddSingleton<Application.Users.Abstractions.IUserRepository, FakeUserRepository>();

                services.RemoveAll(typeof(IUnitOfWork));
                services.AddSingleton<IUnitOfWork, FakeUnitOfWork>();

                services.PostConfigure<JwtOptions>(o =>
                {
                    if (string.IsNullOrWhiteSpace(o.Key)) o.Key = new string('k', 32);
                    if (string.IsNullOrWhiteSpace(o.Issuer)) o.Issuer = "Test";
                    if (string.IsNullOrWhiteSpace(o.Audience)) o.Audience = "Test";
                    if (o.ExpMinutes <= 0) o.ExpMinutes = 60;
                });
            });
        }
    }
}
