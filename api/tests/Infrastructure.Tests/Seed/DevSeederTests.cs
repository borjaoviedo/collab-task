using Application.Common.Abstractions.Security;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data.Seeders;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestHelpers;

namespace Infrastructure.Tests.Seed
{
    [Collection("SqlServerContainer")]
    public sealed class DevSeederTests(MsSqlContainerFixture fx)
    {
        private readonly MsSqlContainerFixture _fx = fx;
        private readonly string _cs = fx.ConnectionString;

        [Fact]
        public async Task Seed_Creates_Demo_Users_And_Project()
        {
            await _fx.ResetAsync();
            var (sp, db) = DbHelper.BuildDb(_cs);

            await DevSeeder.SeedAsync(sp);

            using var s = sp.CreateScope();
            var provider = s.ServiceProvider;

            var emails = await db.Users.AsNoTracking().Select(u => u.Email.Value).ToListAsync();
            emails.Should().Contain(["admin@demo.com", "user@demo.com"]);

            var hasher = provider.GetRequiredService<IPasswordHasher>();
            var admin = await db.Users.AsNoTracking().SingleAsync(u => u.Email == Email.Create("admin@demo.com"));
            hasher.Verify("Admin123!", admin.PasswordSalt, admin.PasswordHash).Should().BeTrue();

            (await db.Projects.AsNoTracking().CountAsync(p => p.Slug == ProjectSlug.Create("demo-project"))).Should().Be(1);
            (await db.ProjectMembers.AsNoTracking().CountAsync()).Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Seed_Is_Idempotent()
        {
            await _fx.ResetAsync();
            var (sp, db) = DbHelper.BuildDb(_cs);

            await DevSeeder.SeedAsync(sp);
            await DevSeeder.SeedAsync(sp);

            (await db.Users.CountAsync(u => u.Email == Email.Create("admin@demo.com"))).Should().Be(1);
            (await db.Users.CountAsync(u => u.Email == Email.Create("user@demo.com"))).Should().Be(1);
            (await db.Projects.CountAsync(p => p.Slug == ProjectSlug.Create("demo-project"))).Should().Be(1);
        }
    }
}
