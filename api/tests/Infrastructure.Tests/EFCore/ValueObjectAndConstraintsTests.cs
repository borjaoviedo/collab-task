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
            var (sp, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = Email.Create("vo@demo.com"),
                PasswordHash = new byte[] { 1, 2, 3 },
                PasswordSalt = new byte[] { 4, 5, 6 },
                Role = UserRole.User
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var fromDb = await db.Users.SingleAsync(u => u.Id == user.Id);
            fromDb.Email.Value.Should().Be("vo@demo.com");
        }

        [Fact]
        public async Task Project_Slug_Is_Unique()
        {
            var (sp, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var p1 = new Project { Id = Guid.NewGuid(), Name = ProjectName.Create("A"), Slug = ProjectSlug.Create("unique") };
            var p2 = new Project { Id = Guid.NewGuid(), Name = ProjectName.Create("B"), Slug = ProjectSlug.Create("unique") };

            db.Projects.Add(p1);
            await db.SaveChangesAsync();

            db.Projects.Add(p2);
            Func<Task> act = async () => await db.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task ProjectMember_RemovedAt_Can_Be_Null_And_CheckConstraint_Enforced()
        {
            var (sp, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var p = new Project { Id = Guid.NewGuid(), Name = ProjectName.Create("P"), Slug = ProjectSlug.Create("p") };
            var u1 = new User { Id = Guid.NewGuid(), Email = Email.Create("pm1@demo.com"), PasswordHash = new byte[] { 1 }, PasswordSalt = new byte[] { 1 }, Role = UserRole.User };
            var u2 = new User { Id = Guid.NewGuid(), Email = Email.Create("pm2@demo.com"), PasswordHash = new byte[] { 2 }, PasswordSalt = new byte[] { 2 }, Role = UserRole.User };
            db.AddRange(p, u1, u2);
            await db.SaveChangesAsync();

            var pmOk = new ProjectMember
            {
                ProjectId = p.Id,
                UserId = u1.Id,
                Role = ProjectRole.Editor,
                JoinedAt = DateTimeOffset.UtcNow,
                RemovedAt = null
            };
            db.ProjectMembers.Add(pmOk);
            await db.SaveChangesAsync(); // should succeed

            var pmBad = new ProjectMember
            {
                ProjectId = p.Id,
                UserId = u2.Id,
                Role = ProjectRole.Editor,
                JoinedAt = DateTimeOffset.UtcNow,
                RemovedAt = DateTimeOffset.UtcNow.AddMinutes(-5) // earlier than JoinedAt should violate CHECK
            };
            db.ProjectMembers.Add(pmBad);

            Func<Task> act = async () => await db.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }
}
