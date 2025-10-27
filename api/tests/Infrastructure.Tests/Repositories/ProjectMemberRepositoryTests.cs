using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using TestHelpers;

namespace Infrastructure.Tests.Repositories
{
    public sealed class ProjectMemberRepositoryTests
    {
        [Fact]
        public async Task GetByProjectAndUserIdAsync_Returns_Member_When_Exists_Else_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (pId, uId) = TestDataFactory.SeedUserWithProject(db);

            var found = await repo.GetByProjectAndUserIdAsync(pId, uId);
            found.Should().NotBeNull();
            found!.Role.Should().Be(ProjectRole.Owner);

            var missing = await repo.GetByProjectAndUserIdAsync(pId, Guid.NewGuid());
            missing.Should().BeNull();
        }

        [Fact]
        public async Task GetTrackedByProjectAndUserIdAsync_Returns_Member_When_Exists_Else_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (pId, uId) = TestDataFactory.SeedUserWithProject(db);

            var found = await repo.GetTrackedByProjectAndUserIdAsync(pId, uId);
            found.Should().NotBeNull();
            found!.Role.Should().Be(ProjectRole.Owner);

            var missing = await repo.GetTrackedByProjectAndUserIdAsync(pId, Guid.NewGuid());
            missing.Should().BeNull();
        }

        [Fact]
        public async Task ListByProjectAsync_Returns_Members_List_When_Exists()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (pId, _) = TestDataFactory.SeedUserWithProject(db);
            var list = await repo.ListByProjectAsync(pId);
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
            var uow = new UnitOfWork(db);

            var (pId, _) = TestDataFactory.SeedUserWithProject(db);
            var newUser = TestDataFactory.SeedUser(db);
            var newProjectMember = ProjectMember.Create(pId, newUser.Id, ProjectRole.Member);

            await repo.AddAsync(newProjectMember);
            await uow.SaveAsync(MutationKind.Create);

            var fromDb = await repo.GetByProjectAndUserIdAsync(pId, newUser.Id);
            fromDb!.Role.Should().Be(ProjectRole.Member);
        }

        [Fact]
        public async Task UpdateRoleAsync_NoOp_When_Role_Is_The_Same()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (pId, uId) = TestDataFactory.SeedUserWithProject(db);

            var current = await repo.GetByProjectAndUserIdAsync(pId, uId);
            var res = await repo.UpdateRoleAsync(pId, uId, ProjectRole.Owner, current!.RowVersion!);

            res.Should().Be(PrecheckStatus.NoOp);
        }

        [Fact]
        public async Task SetRemovedAsync_Toggles_RemovedAt()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var uow = new UnitOfWork(db);

            var (pId, uId) = TestDataFactory.SeedUserWithProject(db);
            var current = await repo.GetByProjectAndUserIdAsync(pId, uId);

            // remove
            var removeResult = await repo.SetRemovedAsync(pId, uId, current!.RowVersion);
            removeResult.Should().Be(PrecheckStatus.Ready);

            await uow.SaveAsync(MutationKind.Update);

            var removed = await repo.GetByProjectAndUserIdAsync(pId, uId);
            removed!.RemovedAt.Should().NotBeNull();

            // restore with stale token should fail on SaveChanges
            var restoreResult = await repo.SetRestoredAsync(pId, uId, removed!.RowVersion);
            restoreResult.Should().Be(PrecheckStatus.Ready);

            await uow.SaveAsync(MutationKind.Update);

            var restored = await repo.GetByProjectAndUserIdAsync(pId, uId);
            restored!.RemovedAt.Should().Be(null);
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
