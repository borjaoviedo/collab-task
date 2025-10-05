using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.EFCore
{
    [Collection("SqlServerContainer")]
    public sealed class ValueObjectAndConstraintsTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;
        public ValueObjectAndConstraintsTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

        public static byte[] Bytes(int n, byte fill = 0x5A) => Enumerable.Repeat(fill, n).ToArray();
        private (ServiceProvider sp, AppDbContext db) BuildDb(string name)
        {
            var cs = $"{_baseCs};Database={name}";
            var sc = new ServiceCollection();
            sc.AddInfrastructure(cs);
            var sp = sc.BuildServiceProvider();
            var db = sp.GetRequiredService<AppDbContext>();
            return (sp, db);
        }

        [Fact]
        public async Task Email_ValueObject_RoundTrip_Works()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var user = User.Create(Email.Create("vo@demo.com"), UserName.Create("Value Object"), Bytes(32), Bytes(16));

            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var fromDb = await db.Users.SingleAsync(u => u.Id == user.Id);
            fromDb.Email.Value.Should().Be("vo@demo.com");
        }

        [Fact]
        public async Task UserName_ValueObject_RoundTrip_Works()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var user = User.Create(Email.Create("vo@demo.com"), UserName.Create("Value Object Name"), Bytes(32), Bytes(16));

            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var fromDb = await db.Users.SingleAsync(u => u.Id == user.Id);
            fromDb.Name.Value.Should().Be("Value Object Name");
        }

        [Fact]
        public async Task Project_Slug_Is_Unique()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var u = User.Create(Email.Create("m@demo.com"), UserName.Create("Project user"), Bytes(32), Bytes(16));
            db.Users.Add(u);
            await db.SaveChangesAsync();

            var p1 = Project.Create(u.Id, ProjectName.Create("Unique Slug 1"), DateTimeOffset.UtcNow);
            p1.Slug = ProjectSlug.Create("unique");

            db.Projects.Add(p1);
            await db.SaveChangesAsync();

            var p2 = Project.Create(u.Id, ProjectName.Create("Unique Slug 2"), DateTimeOffset.UtcNow);
            p2.Slug = ProjectSlug.Create("unique");

            db.Projects.Add(p2);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        [Fact]
        public async Task ProjectMember_RemovedAt_Can_Be_Null_And_CheckConstraint_Enforced()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var u1 = User.Create(Email.Create("pm1@demo.com"), UserName.Create("First user"), Bytes(32), Bytes(16));
            var p = Project.Create(u1.Id, ProjectName.Create("Project Name"), DateTimeOffset.UtcNow);
            db.AddRange(u1, p);
            await db.SaveChangesAsync();

            var u2 = User.Create(Email.Create("pm2@demo.com"), UserName.Create("Second user"), Bytes(32), Bytes(16));
            db.Users.Add(u2);
            await db.SaveChangesAsync();

            var utcNow = DateTimeOffset.UtcNow;

            var pmBad = new ProjectMember(p.Id, u2.Id, ProjectRole.Member, DateTimeOffset.UtcNow)
            {
                RemovedAt = utcNow.AddMinutes(-5)
            };
            db.ProjectMembers.Add(pmBad);

            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }
}
