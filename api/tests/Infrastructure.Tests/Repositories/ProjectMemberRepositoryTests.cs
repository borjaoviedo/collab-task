using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Repositories
{
    public sealed class ProjectMemberRepositoryTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;
        public ProjectMemberRepositoryTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

        private (ServiceProvider sp, AppDbContext db, ProjectMemberRepository repo) BuildDb(string name)
        {
            var cs = $"{_baseCs};Database={name}";
            var sc = new ServiceCollection();
            sc.AddInfrastructure(cs);
            var sp = sc.BuildServiceProvider();
            var db = sp.GetRequiredService<AppDbContext>();
            var repo = new ProjectMemberRepository(db);
            return (sp, db, repo);
        }

        private static byte[] Bytes(int n) => Enumerable.Range(0, n).Select(_ => (byte)Random.Shared.Next(0, 255)).ToArray();

        private static (User owner, User member, Project project) NewGraph(string project, string ownerName, string memberName, DateTimeOffset now)
        {
            var owner = User.Create(Email.Create($"{Guid.NewGuid():N}@demo.com"), UserName.Create(ownerName), Bytes(32), Bytes(16));
            var mem = User.Create(Email.Create($"{Guid.NewGuid():N}@demo.com"), UserName.Create(memberName), Bytes(32), Bytes(16));
            var p = Project.Create(owner.Id, ProjectName.Create(project), now);
            return (owner, mem, p);
        }

        [Fact]
        public async Task GetAsync_Returns_Member_When_Exists_Else_Null()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner, member, p) = NewGraph("Alpha", "Owner", "Member", now);
            p.AddMember(member.Id, ProjectRole.Member, now);

            db.AddRange(owner, member, p);
            await db.SaveChangesAsync();

            var found = await repo.GetAsync(p.Id, member.Id);
            found.Should().NotBeNull();
            found!.Role.Should().Be(ProjectRole.Member);

            var missing = await repo.GetAsync(p.Id, Guid.NewGuid());
            missing.Should().BeNull();
        }

        [Fact]
        public async Task ExistsAsync_True_When_Exists_False_Otherwise()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner, member, p) = NewGraph("Alpha", "Owner", "Member", now);
            p.AddMember(member.Id, ProjectRole.Member, now);

            db.AddRange(owner, member, p);
            await db.SaveChangesAsync();

            (await repo.ExistsAsync(p.Id, member.Id)).Should().BeTrue();
            (await repo.ExistsAsync(p.Id, Guid.NewGuid())).Should().BeFalse();
        }

        [Fact]
        public async Task AddAsync_Persists_New_Member()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner, member, p) = NewGraph("Alpha", "Owner", "Member", now);

            db.AddRange(owner, member, p);
            await db.SaveChangesAsync();

            var pm = ProjectMember.Create(p.Id, member.Id, ProjectRole.Member, now);
            await repo.AddAsync(pm);
            await db.SaveChangesAsync();

            var fromDb = await db.ProjectMembers.AsNoTracking().SingleAsync(x => x.ProjectId == p.Id && x.UserId == member.Id);
            fromDb.Role.Should().Be(ProjectRole.Member);
        }

        [Fact]
        public async Task UpdateRoleAsync_NoOp_When_Role_Is_The_Same()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner, member, p) = NewGraph("Alpha", "Owner", "Member", now);
            p.AddMember(member.Id, ProjectRole.Admin, now);

            db.AddRange(owner, member, p);
            await db.SaveChangesAsync();

            var current = await db.ProjectMembers.AsNoTracking().SingleAsync(x => x.ProjectId == p.Id && x.UserId == member.Id);
            var res = await repo.UpdateRoleAsync(p.Id, member.Id, ProjectRole.Admin, current.RowVersion!);

            res.Should().Be(DomainMutation.NoOp);
            (await db.SaveChangesAsync()).Should().Be(0);
        }

        [Fact]
        public async Task UpdateRoleAsync_Updated_But_Save_Throws_On_RowVersion_Mismatch()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner, member, p) = NewGraph("Alpha", "Owner", "Member", now);
            p.AddMember(member.Id, ProjectRole.Member, now);

            db.AddRange(owner, member, p);
            await db.SaveChangesAsync();

            var res = await repo.UpdateRoleAsync(p.Id, member.Id, ProjectRole.Admin, new byte[] { 1, 2, 3, 4 });
            res.Should().Be(DomainMutation.Updated);

            await FluentActions.Invoking(() => db.SaveChangesAsync())
                .Should().ThrowAsync<DbUpdateConcurrencyException>();
        }

        [Fact]
        public async Task SetRemovedAsync_Toggles_RemovedAt_And_Uses_Concurrency_Token()
        {
            var (_, db, repo) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var (owner, member, p) = NewGraph("Alpha", "Owner", "Member", now);
            p.AddMember(member.Id, ProjectRole.Member, now);

            db.AddRange(owner, member, p);
            await db.SaveChangesAsync();

            var current = await db.ProjectMembers.AsNoTracking().SingleAsync(x => x.ProjectId == p.Id && x.UserId == member.Id);

            // remove
            var res1 = await repo.SetRemovedAsync(p.Id, member.Id, removedAt: now.AddMinutes(5), rowVersion: current.RowVersion!);
            res1.Should().Be(DomainMutation.Updated);
            await db.SaveChangesAsync();

            var removed = await db.ProjectMembers.AsNoTracking().SingleAsync(x => x.ProjectId == p.Id && x.UserId == member.Id);
            removed.RemovedAt.Should().NotBeNull();

            // restore with stale token should fail on SaveChanges
            var res2 = await repo.SetRemovedAsync(p.Id, member.Id, removedAt: null, rowVersion: new byte[] { 9, 9, 9 });
            res2.Should().Be(DomainMutation.Updated);

            await FluentActions.Invoking(() => db.SaveChangesAsync())
                .Should().ThrowAsync<DbUpdateConcurrencyException>();
        }
    }
}
