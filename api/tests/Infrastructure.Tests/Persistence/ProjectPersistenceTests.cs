using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Persistence
{
    public sealed class ProjectPersistenceTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;
        public ProjectPersistenceTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

        private (ServiceProvider sp, AppDbContext db) BuildDb(string name)
        {
            var cs = $"{_baseCs};Database={name}";
            var sc = new ServiceCollection();
            sc.AddInfrastructure(cs);
            var sp = sc.BuildServiceProvider();
            var db = sp.GetRequiredService<AppDbContext>();
            return (sp, db);
        }

        private static byte[] Bytes(int n) => Enumerable.Range(0, n).Select(_ => (byte)Random.Shared.Next(0, 255)).ToArray();

        private static (User owner, Project project) NewProject(string projectName, string userName, DateTimeOffset now)
        {
            var owner = User.Create(Email.Create($"{Guid.NewGuid():N}@demo.com"), UserName.Create(userName), Bytes(32), Bytes(16));
            var project = Project.Create(owner.Id, ProjectName.Create(projectName), now);
            return (owner, project);
        }

        [Fact]
        public async Task ProjectName_ValueObject_RoundTrip_Works()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var (owner, p) = NewProject("Alpha Board", "Owner", DateTimeOffset.UtcNow);
            db.AddRange(owner, p);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var fromDb = await db.Projects.SingleAsync(x => x.Id == p.Id);
            fromDb.Name.Value.Should().Be("Alpha Board");
        }

        [Fact]
        public async Task Unique_Index_OwnerId_Slug_Is_Enforced()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner, p1) = NewProject("Alpha", "Owner", now);
            var p2 = Project.Create(owner.Id, ProjectName.Create("Alpha"), now.AddMinutes(1)); // same slug
            db.AddRange(owner, p1, p2);

            Func<Task> act = async () => await db.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task Members_Are_Deleted_On_Project_Delete_Cascade()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner, p) = NewProject("Alpha", "Owner", now);
            var u2 = User.Create(Email.Create("u2@demo.com"), UserName.Create("Member"), Bytes(32), Bytes(16));
            p.AddMember(u2.Id, ProjectRole.Member, now);

            db.AddRange(owner, u2, p);
            await db.SaveChangesAsync();

            var membersBefore = await db.ProjectMembers.CountAsync(m => m.ProjectId == p.Id);
            membersBefore.Should().BeGreaterThan(0); // owner membership may exist too

            db.Projects.Remove(p);
            await db.SaveChangesAsync();

            var membersAfter = await db.ProjectMembers.CountAsync(m => m.ProjectId == p.Id);
            membersAfter.Should().Be(0);
        }

        [Fact]
        public async Task RowVersion_Changes_On_Update()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var (owner, p) = NewProject("Alpha", "Owner", DateTimeOffset.UtcNow);
            db.AddRange(owner, p);
            await db.SaveChangesAsync();

            var v1 = p.RowVersion;

            // Update any mapped property to trigger rowversion
            p = await db.Projects.SingleAsync(x => x.Id == p.Id);
            // rename via VO to ensure state is Modified
            typeof(Project).GetProperty(nameof(Project.Name))!
                .SetValue(p, ProjectName.Create("Alpha Renamed"));
            await db.SaveChangesAsync();

            var refreshed = await db.Projects.SingleAsync(x => x.Id == p.Id);
            refreshed.RowVersion.Should().NotBeNull();
            refreshed.RowVersion.Should().NotEqual(v1);
        }

        [Fact]
        public async Task Check_UpdatedAt_GreaterOrEqual_CreatedAt_Is_Enforced()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var (owner, p) = NewProject("Alpha", "Owner", DateTimeOffset.UtcNow);
            db.AddRange(owner, p);
            await db.SaveChangesAsync();

            var entry = db.Entry(p);
            entry.Property(nameof(Project.CreatedAt)).CurrentValue = p.UpdatedAt.AddHours(1);
            entry.Property(nameof(Project.CreatedAt)).IsModified = true;

            Func<Task> act = async () => await db.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }
}
