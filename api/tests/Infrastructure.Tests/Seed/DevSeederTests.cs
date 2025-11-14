using Application.Abstractions.Security;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence.Seeders;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestHelpers.Persistence;

namespace Infrastructure.Tests.Seed
{
    [Collection("SqlServerContainer")]
    public sealed class DevSeederTests(MsSqlContainerFixture fx)
    {
        private readonly MsSqlContainerFixture _fx = fx;
        private readonly string _cs = fx.ConnectionString;

        [Fact]
        public async Task Seed_Creates_Demo_Data_With_Two_Projects_And_Board()
        {
            await _fx.ResetAsync();
            var (sp, db) = DbHelper.BuildDb(_cs);

            await DevSeeder.SeedAsync(sp);

            using var scope = sp.CreateScope();
            var provider = scope.ServiceProvider;
            var hasher = provider.GetRequiredService<IPasswordHasher>();

            // Users
            var emails = await db.Users
                .AsNoTracking()
                .Select(u => u.Email.Value)
                .ToListAsync();
            emails.Should().Contain(["admin@demo.com", "user@demo.com", "guest@demo.com"]);

            var admin = await db.Users
                .AsNoTracking()
                .SingleAsync(u => u.Email == Email.Create("admin@demo.com"));
            hasher.Verify("Admin123!", admin.PasswordSalt, admin.PasswordHash).Should().BeTrue();

            var user = await db.Users
                .AsNoTracking()
                .SingleAsync(u => u.Email == Email.Create("user@demo.com"));
            hasher.Verify("User123!", user.PasswordSalt, user.PasswordHash).Should().BeTrue();

            var guest = await db.Users
                .AsNoTracking()
                .SingleAsync(u => u.Email == Email.Create("guest@demo.com"));
            hasher.Verify("Guest123!", guest.PasswordSalt, guest.PasswordHash).Should().BeTrue();

            // Projects
            (await db.Projects.AsNoTracking().CountAsync(p => p.Slug == ProjectSlug.Create("demo-project-a"))).Should().Be(1);
            (await db.Projects.AsNoTracking().CountAsync(p => p.Slug == ProjectSlug.Create("demo-project-b"))).Should().Be(1);

            // Memberships
            (await db.ProjectMembers.AsNoTracking().CountAsync()).Should().BeGreaterThan(0);

            // Board primitives
            (await db.Lanes.AsNoTracking().CountAsync()).Should().BeGreaterThan(0);
            (await db.Columns.AsNoTracking().CountAsync()).Should().BeGreaterThan(0);

            // Tasks and related
            (await db.TaskItems.AsNoTracking().CountAsync()).Should().BeGreaterThan(0);
            (await db.TaskAssignments.AsNoTracking().CountAsync()).Should().BeGreaterThan(0);
            (await db.TaskNotes.AsNoTracking().CountAsync()).Should().BeGreaterThan(0);
            (await db.TaskActivities.AsNoTracking().CountAsync()).Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Seed_Is_Idempotent()
        {
            await _fx.ResetAsync();
            var (sp, db) = DbHelper.BuildDb(_cs);

            await DevSeeder.SeedAsync(sp);
            await DevSeeder.SeedAsync(sp);

            // Users remain unique
            (await db.Users.CountAsync(u => u.Email == Email.Create("admin@demo.com"))).Should().Be(1);
            (await db.Users.CountAsync(u => u.Email == Email.Create("user@demo.com"))).Should().Be(1);
            (await db.Users.CountAsync(u => u.Email == Email.Create("guest@demo.com"))).Should().Be(1);

            // Projects remain singletons
            (await db.Projects.CountAsync(p => p.Slug == ProjectSlug.Create("demo-project-a"))).Should().Be(1);
            (await db.Projects.CountAsync(p => p.Slug == ProjectSlug.Create("demo-project-b"))).Should().Be(1);

            // No duplication on board/task artifacts
            // Sanity: totals unchanged after second run
            var lanes = await db.Lanes.AsNoTracking().CountAsync();
            var columns = await db.Columns.AsNoTracking().CountAsync();
            var tasks = await db.TaskItems.AsNoTracking().CountAsync();
            var notes = await db.TaskNotes.AsNoTracking().CountAsync();
            var assigns = await db.TaskAssignments.AsNoTracking().CountAsync();
            var acts = await db.TaskActivities.AsNoTracking().CountAsync();

            await DevSeeder.SeedAsync(sp);

            (await db.Lanes.AsNoTracking().CountAsync()).Should().Be(lanes);
            (await db.Columns.AsNoTracking().CountAsync()).Should().Be(columns);
            (await db.TaskItems.AsNoTracking().CountAsync()).Should().Be(tasks);
            (await db.TaskNotes.AsNoTracking().CountAsync()).Should().Be(notes);
            (await db.TaskAssignments.AsNoTracking().CountAsync()).Should().Be(assigns);
            (await db.TaskActivities.AsNoTracking().CountAsync()).Should().Be(acts);
        }
    }
}
