using Api.Tests.Fakes;
using Application.ProjectMembers.Abstractions;
using Application.Projects.Abstractions;
using Application.Users.Abstractions;
using Infrastructure.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

                services.RemoveAll(typeof(IUserRepository));
                services.AddSingleton<IUserRepository, FakeUserRepository>();

                services.RemoveAll(typeof(IProjectRepository));
                services.AddSingleton<IProjectRepository, FakeProjectRepository>();

                services.RemoveAll(typeof(IProjectMemberRepository));
                services.AddSingleton<IProjectMemberRepository, FakeProjectMemberRepository>();

                services.RemoveAll(typeof(IProjectMemberReadService));
                services.AddSingleton<IProjectMemberReadService, FakeProjectMemberReadService>();

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
