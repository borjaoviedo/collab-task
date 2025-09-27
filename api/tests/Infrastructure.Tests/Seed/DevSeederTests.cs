using Application.Common.Abstractions.Security;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Extensions;
using Infrastructure.Data.Seeders;
using Infrastructure.Tests.Containers;
using Infrastructure.Tests.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Seed
{
    [Collection(nameof(DbCollection))]
    public class DevSeederTests
    {
        private readonly MsSqlContainerFixture _fx;
        public DevSeederTests(MsSqlContainerFixture fx) => _fx = fx;

        [Fact]
        public async Task Seed_Creates_Demo_Users_And_Project()
        {
            var baseCs = _fx.ContainerConnectionString;
            var dbName = $"CollabTaskTest_{Guid.NewGuid():N}";
            var cs = $"{baseCs};Database={dbName}";

            var sc = new ServiceCollection();
            sc.AddInfrastructure(cs);
            var services = sc.BuildServiceProvider();

            using (var scope = services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.MigrateAsync();
            }
            await DevSeeder.SeedAsync(services);

            using var s2 = services.CreateScope();
            var sp = s2.ServiceProvider;
            var db2 = sp.GetRequiredService<AppDbContext>();

            var users = await db2.Users.AsNoTracking().ToListAsync();
            users.Select(u => u.Email.Value)
                 .Should().Contain(["admin@demo.com", "user@demo.com"]);

            var hasher = sp.GetRequiredService<IPasswordHasher>();
            var admin = users.Single(u => u.Email.Value == "admin@demo.com");
            hasher.Verify("Admin123!", admin.PasswordHash, admin.PasswordSalt).Should().BeTrue();

            (await db2.Projects.AsNoTracking().CountAsync(p => p.Slug == ProjectSlug.Create("demo-project"))).Should().Be(1);
            (await db2.ProjectMembers.AsNoTracking().CountAsync()).Should().BeGreaterThan(0);
        }
    }
}
