using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Common;
using TestHelpers.Common.Time;
using TestHelpers.Persistence;

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

            var (projectId, userId) = TestDataFactory.SeedUserWithProject(db);

            var found = await repo.GetByProjectAndUserIdAsync(projectId, userId);
            found.Should().NotBeNull();
            found.Role.Should().Be(ProjectRole.Owner);

            var missing = await repo.GetByProjectAndUserIdAsync(projectId, userId: Guid.NewGuid());
            missing.Should().BeNull();
        }

        [Fact]
        public async Task GetByProjectAndUserIdForUpdateAsync_Returns_Member_When_Exists_Else_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (projectId, userId) = TestDataFactory.SeedUserWithProject(db);

            var found = await repo.GetByProjectAndUserIdForUpdateAsync(projectId, userId);
            found.Should().NotBeNull();
            found.Role.Should().Be(ProjectRole.Owner);

            var missing = await repo.GetByProjectAndUserIdForUpdateAsync(projectId, userId: Guid.NewGuid());
            missing.Should().BeNull();
        }

        [Fact]
        public async Task ListByProjectIdAsync_Returns_Members_List_When_Exists()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var list = await repo.ListByProjectIdAsync(projectId);

            list.Should().NotBeEmpty();
            list.Count.Should().Be(1);
        }

        [Fact]
        public async Task ExistsAsync_True_When_Exists_False_Otherwise()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (projectId, userId) = TestDataFactory.SeedUserWithProject(db);

            var existingMemberExists = await repo.ExistsAsync(projectId, userId);
            existingMemberExists.Should().BeTrue();

            var nonExistingMemberExists = await repo.ExistsAsync(projectId, userId: Guid.NewGuid());
            nonExistingMemberExists.Should().BeFalse();
        }

        [Fact]
        public async Task CountUserActiveMembershipsAsync_Returns_Only_Active_Memberships_Count()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);

            var count = await repo.CountUserActiveMembershipsAsync(userId);
            count.Should().Be(1);

            TestDataFactory.SeedProject(db, userId);
            count = await repo.CountUserActiveMembershipsAsync(userId);
            count.Should().Be(2);

            var (otherProjectId, _) = TestDataFactory.SeedUserWithProject(db);
            var member = TestDataFactory.SeedProjectMember(db, otherProjectId, userId);

            count = await repo.CountUserActiveMembershipsAsync(userId);
            count.Should().Be(3);

            member.Remove(removedAtUtc: DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            count = await repo.CountUserActiveMembershipsAsync(userId);
            count.Should().Be(2);
        }

        [Fact]
        public async Task GetUserRoleAsync_Returns_Role_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (projectId, userId) = TestDataFactory.SeedUserWithProject(db);

            var role = await repo.GetUserRoleAsync(projectId, userId);
            role.Should().NotBeNull();
            role.Should().Be(ProjectRole.Owner);

            var nullRole = await repo.GetUserRoleAsync(projectId, userId: Guid.NewGuid());
            nullRole.Should().BeNull();
        }

        // --------------- Add / Update ---------------

        [Fact]
        public async Task AddAsync_Persists_New_Member()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var uow = new UnitOfWork(db);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var newUser = TestDataFactory.SeedUser(db);
            var newProjectMember = ProjectMember.Create(projectId, newUser.Id, ProjectRole.Member);

            await repo.AddAsync(newProjectMember);
            await uow.SaveAsync(MutationKind.Create);

            var fromDb = await repo.GetByProjectAndUserIdAsync(projectId, newUser.Id);
            fromDb!.Role.Should().Be(ProjectRole.Member);
        }

        [Fact]
        public async Task UpdateAsync_Marks_Entity_Modified_And_Persists_Changes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);

            var (projectId, userId) = TestDataFactory.SeedUserWithProject(db);
            var projectMember = await repo.GetByProjectAndUserIdForUpdateAsync(projectId, userId);

            // Modify through domain behavior
            var now = DateTimeOffset.UtcNow;
            projectMember!.Remove(now);

            await repo.UpdateAsync(projectMember);
            await db.SaveChangesAsync();

            var reloaded = await db.ProjectMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId);

            reloaded.Should().NotBeNull();
            reloaded!.RemovedAt.Should().BeCloseTo(now, TimeSpan.FromMilliseconds(1));
        }
    }
}
