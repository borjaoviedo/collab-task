using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using TestHelpers;

namespace Infrastructure.Tests.Repositories
{
    public sealed class ProjectMemberRepositoryTests
    {
        [Fact]
        public async Task GetAsync_Returns_Member_When_Exists_Else_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (pId, uId) = TestDataFactory.SeedUserWithProject(db);

            var found = await repo.GetAsync(pId, uId);
            found.Should().NotBeNull();
            found!.Role.Should().Be(ProjectRole.Owner);

            var missing = await repo.GetAsync(pId, Guid.NewGuid());
            missing.Should().BeNull();
        }

        [Fact]
        public async Task GetTrackedByIdAsync_Returns_Member_When_Exists_Else_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (pId, uId) = TestDataFactory.SeedUserWithProject(db);

            var found = await repo.GetTrackedByIdAsync(pId, uId);
            found.Should().NotBeNull();
            found!.Role.Should().Be(ProjectRole.Owner);

            var missing = await repo.GetTrackedByIdAsync(pId, Guid.NewGuid());
            missing.Should().BeNull();
        }

        [Fact]
        public async Task GetByProjectAsync_Returns_Members_List_When_Exists()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (pId, _) = TestDataFactory.SeedUserWithProject(db);
            var list = await repo.GetByProjectAsync(pId);
            list.Should().NotBeEmpty();
            list.Count.Should().Be(1);
        }

        [Fact]
        public async Task ExistsAsync_True_When_Exists_False_Otherwise()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (pId, uId) = TestDataFactory.SeedUserWithProject(db);

            (await repo.ExistsAsync(pId, uId)).Should().BeTrue();
            (await repo.ExistsAsync(pId, Guid.NewGuid())).Should().BeFalse();
        }

        [Fact]
        public async Task AddAsync_Persists_New_Member()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (pId, _) = TestDataFactory.SeedUserWithProject(db);
            var newUser = TestDataFactory.SeedUser(db);
            var newProjectMember = ProjectMember.Create(pId, newUser.Id, ProjectRole.Member, DateTimeOffset.UtcNow);

            await repo.AddAsync(newProjectMember);
            await db.SaveChangesAsync();

            var fromDb = await repo.GetAsync(pId, newUser.Id);
            fromDb!.Role.Should().Be(ProjectRole.Member);
        }

        [Fact]
        public async Task UpdateRoleAsync_NoOp_When_Role_Is_The_Same()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (pId, uId) = TestDataFactory.SeedUserWithProject(db);

            var current = await repo.GetAsync(pId, uId);
            var res = await repo.UpdateRoleAsync(pId, uId, ProjectRole.Owner, current!.RowVersion!);

            res.Should().Be(DomainMutation.NoOp);
        }

        [Fact]
        public async Task UpdateRoleAsync_Returns_Conflict_On_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (pId, uId) = TestDataFactory.SeedUserWithProject(db);

            var res = await repo.UpdateRoleAsync(pId, uId, ProjectRole.Admin, [1, 2]);
            res.Should().Be(DomainMutation.Conflict);
        }

        [Fact]
        public async Task SetRemovedAsync_Toggles_RemovedAt()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (pId, uId) = TestDataFactory.SeedUserWithProject(db);
            var current = await repo.GetAsync(pId, uId);

            // remove
            var removedAt = DateTimeOffset.UtcNow.AddMinutes(5);
            var removeResult = await repo.SetRemovedAsync(pId, uId, removedAt, current!.RowVersion);
            removeResult.Should().Be(DomainMutation.Updated);

            var removed = await repo.GetAsync(pId, uId);
            removed!.RemovedAt.Should().NotBeNull();
            removed!.RemovedAt.Should().Be(removedAt);

            // restore with stale token should fail on SaveChanges
            var restoreResult = await repo.SetRemovedAsync(pId, uId, null, removed!.RowVersion);
            restoreResult.Should().Be(DomainMutation.Updated);

            var restored = await repo.GetAsync(pId, uId);
            restored!.RemovedAt.Should().Be(null);
        }

        [Fact]
        public async Task SetRemovedAsync_Returns_Conflict_On_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (pId, uId) = TestDataFactory.SeedUserWithProject(db);

            // remove
            var removeResult = await repo.SetRemovedAsync(pId, uId, DateTimeOffset.UtcNow.AddMinutes(5), [1, 2, 3]);
            removeResult.Should().Be(DomainMutation.Conflict);
        }

        [Fact]
        public async Task CountUserActiveMembershipsAsync_Returns_Only_Active_Memberships_Count()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (_, uId) = TestDataFactory.SeedUserWithProject(db);

            var count = await repo.CountUserActiveMembershipsAsync(uId);
            count.Should().Be(1);

            TestDataFactory.SeedProject(db, uId);
            count = await repo.CountUserActiveMembershipsAsync(uId);
            count.Should().Be(2);

            var (otherProjectId, _) = TestDataFactory.SeedUserWithProject(db);
            var pm = TestDataFactory.SeedProjectMember(db, otherProjectId, uId);
            count = await repo.CountUserActiveMembershipsAsync(uId);
            count.Should().Be(3);

            pm.Remove(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            count = await repo.CountUserActiveMembershipsAsync(uId);
            count.Should().Be(2);
        }

        [Fact]
        public async Task GetRoleAsync_Returns_Role_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (pId, uId) = TestDataFactory.SeedUserWithProject(db);

            var role = await repo.GetRoleAsync(pId, uId);
            role.Should().NotBeNull();
            role.Should().Be(ProjectRole.Owner);

            var nullRole = await repo.GetRoleAsync(pId, Guid.NewGuid());
            nullRole.Should().BeNull();
        }
    }
}
