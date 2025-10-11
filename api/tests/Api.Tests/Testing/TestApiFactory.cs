using Api.Tests.Fakes;
using Application.ProjectMembers.Abstractions;
using Application.ProjectMembers.Services;
using Application.Projects.Abstractions;
using Application.Projects.Services;
using Application.Users.Abstractions;
using Application.Users.Services;
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

                // User
                services.RemoveAll(typeof(IUserRepository));
                services.AddSingleton<IUserRepository, FakeUserRepository>();

                services.RemoveAll(typeof(IUserReadService));
                services.AddScoped<IUserReadService>(sp =>
                    new UserReadService(sp.GetRequiredService<IUserRepository>()));

                services.RemoveAll(typeof(IUserWriteService));
                services.AddScoped<IUserWriteService>(sp =>
                    new UserWriteService(sp.GetRequiredService<IUserRepository>()));

                // Project
                services.RemoveAll(typeof(IProjectRepository));
                services.AddSingleton<IProjectRepository, FakeProjectRepository>();

                services.RemoveAll(typeof(IProjectReadService));
                services.AddScoped<IProjectReadService>(sp =>
                    new ProjectReadService(sp.GetRequiredService<IProjectRepository>()));

                services.RemoveAll(typeof(IProjectWriteService));
                services.AddScoped<IProjectWriteService>(sp =>
                    new ProjectWriteService(sp.GetRequiredService<IProjectRepository>()));

                // Project member
                services.RemoveAll(typeof(IProjectMemberRepository));
                services.AddSingleton<IProjectMemberRepository, FakeProjectMemberRepository>();

                services.RemoveAll(typeof(IProjectMemberReadService));
                services.AddScoped<IProjectMemberReadService>(sp =>
                    new ProjectMemberReadService(sp.GetRequiredService<IProjectMemberRepository>()));

                services.RemoveAll(typeof(IProjectMemberWriteService));
                services.AddScoped<IProjectMemberWriteService>(sp =>
                    new ProjectMemberWriteService(sp.GetRequiredService<IProjectMemberRepository>()));

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
