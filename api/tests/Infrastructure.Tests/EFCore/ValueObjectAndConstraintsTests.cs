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

            var user = User.Create(Email.Create("vo@demo.com"), UserName.Create("Value Object"), [1, 2, 3], [4, 5, 6]);

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

            var user = User.Create(Email.Create("vo@demo.com"), UserName.Create("Value Object Name"), [1, 2, 3], [4, 5, 6]);

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

            var p1 = Project.Create(Guid.NewGuid(), ProjectName.Create("Unique Slug 1"), DateTimeOffset.UtcNow);
            p1.Slug = ProjectSlug.Create("unique");
            var p2 = Project.Create(Guid.NewGuid(), ProjectName.Create("Unique Slug 2"), DateTimeOffset.UtcNow);
            p2.Slug = ProjectSlug.Create("unique");

            db.Projects.Add(p1);
            await db.SaveChangesAsync();

            db.Projects.Add(p2);
            Func<Task> act = async () => await db.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task ProjectMember_RemovedAt_Can_Be_Null_And_CheckConstraint_Enforced()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var u1 = User.Create(Email.Create("pm1@demo.com"), UserName.Create("First user"), [1], [1]);
            var p = Project.Create(u1.Id, ProjectName.Create("Project Name"), DateTimeOffset.UtcNow);
            var u2 = User.Create(Email.Create("pm2@demo.com"), UserName.Create("Second user"), [2], [2]);
            db.AddRange(p, u1, u2);
            await db.SaveChangesAsync();

            var utcNow = DateTimeOffset.UtcNow;
            var pmOk = new ProjectMember(p.Id, u1.Id, ProjectRole.Member, utcNow);
            db.ProjectMembers.Add(pmOk);
            await db.SaveChangesAsync(); // should succeed

            var pmBad = new ProjectMember(p.Id, u2.Id, ProjectRole.Member, utcNow)
            {
                RemovedAt = utcNow.AddMinutes(-5)
            };
            db.ProjectMembers.Add(pmBad);

            Func<Task> act = async () => await db.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }
}
