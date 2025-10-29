using Application.ProjectMembers.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using TestHelpers;

namespace Application.Tests.ProjectMembers.Services
{
    public sealed class ProjectMemberReadServiceTests
    {
        [Fact]
        public async Task GetAsync_Returns_ProjectMember_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var readSvc = new ProjectMemberReadService(repo);

            var (projectId, userId) = TestDataFactory.SeedUserWithProject(db);

            var existingResult = await readSvc.GetAsync(projectId, userId);
            existingResult.Should().NotBeNull();
            existingResult.ProjectId.Should().Be(projectId);
            existingResult.UserId.Should().Be(userId);

            var notFoundResult = await readSvc.GetAsync(projectId, userId: Guid.Empty);
            notFoundResult.Should().BeNull();
        }

        [Fact]
        public async Task ListByProjectAsync_Returns_All_Users_By_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var readSvc = new ProjectMemberReadService(repo);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);
            var list = await readSvc.ListByProjectAsync(projectId);
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            var newUser = TestDataFactory.SeedUser(db);
            TestDataFactory.SeedProjectMember(db, projectId, newUser.Id);
            list = await readSvc.ListByProjectAsync(projectId);
            list.Count.Should().Be(2);
        }

        [Fact]
        public async Task ListByProjectAsync_Returns_Empty_List_When_Not_Found_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var readSvc = new ProjectMemberReadService(repo);

            var list = await readSvc.ListByProjectAsync(projectId: Guid.NewGuid());
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRoleAsync_Returns_Owner_Role_When_User_Creates_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var readSvc = new ProjectMemberReadService(repo);

            var (projectId, userId) = TestDataFactory.SeedUserWithProject(db);

            var role = await readSvc.GetRoleAsync(projectId, userId);
            role.Should().NotBeNull();
            role.Value.Should().Be(ProjectRole.Owner);
        }

        [Fact]
        public async Task GetRoleAsync_Returns_Null_When_User_Not_In_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var readSvc = new ProjectMemberReadService(repo);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);

            var role = await readSvc.GetRoleAsync(projectId, userId: Guid.NewGuid());
            role.Should().BeNull();
        }

        [Fact]
        public async Task CountActiveAsync_Returns_Expected_Values()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var readSvc = new ProjectMemberReadService(repo);

            var user = TestDataFactory.SeedUser(db);
            var count = await readSvc.CountActiveAsync(user.Id);
            count.Should().Be(0);

            TestDataFactory.SeedProject(db, user.Id);
            count = await readSvc.CountActiveAsync(user.Id);
            count.Should().Be(1);

            var otherUser = TestDataFactory.SeedUser(db);
            var secondProject = TestDataFactory.SeedProject(db, otherUser.Id); // new project with different owner
            count = await readSvc.CountActiveAsync(user.Id);
            count.Should().Be(1);

            TestDataFactory.SeedProjectMember(db, secondProject.Id, user.Id);
            count = await readSvc.CountActiveAsync(user.Id);
            count.Should().Be(2);

            secondProject.RemoveMember(user.Id, removedAtUtc: DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            count = await readSvc.CountActiveAsync(user.Id);
            count.Should().Be(1);
        }
    }
}
