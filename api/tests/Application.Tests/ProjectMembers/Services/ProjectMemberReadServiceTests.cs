using Application.ProjectMembers.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using System.Data;
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
            var svc = new ProjectMemberReadService(repo);

            var (pId, uId) = TestDataFactory.SeedUserWithProject(db);

            var existingResult = await svc.GetAsync(pId, uId);
            existingResult.Should().NotBeNull();
            existingResult.ProjectId.Should().Be(pId);
            existingResult.UserId.Should().Be(uId);

            var notFoundResult = await svc.GetAsync(pId, Guid.Empty);
            notFoundResult.Should().Be(null);
        }

        [Fact]
        public async Task ListByProjectAsync_Returns_All_Users_By_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var svc = new ProjectMemberReadService(repo);

            var (pId, _) = TestDataFactory.SeedUserWithProject(db);
            var list = await svc.ListByProjectAsync(pId);
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            var newUser = TestDataFactory.SeedUser(db);
            TestDataFactory.SeedProjectMember(db, pId, newUser.Id);
            list = await svc.ListByProjectAsync(pId);
            list.Count.Should().Be(2);
        }

        [Fact]
        public async Task ListByProjectAsync_Returns_Empty_List_When_Not_Found_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var svc = new ProjectMemberReadService(repo);

            var list = await svc.ListByProjectAsync(Guid.NewGuid());
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRoleAsync_Returns_Owner_Role_When_User_Creates_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var svc = new ProjectMemberReadService(repo);

            var (pId, uId) = TestDataFactory.SeedUserWithProject(db);

            var role = await svc.GetRoleAsync(pId, uId);
            role.Should().NotBeNull();
            role!.Value.Should().Be(ProjectRole.Owner);
        }

        [Fact]
        public async Task GetRoleAsync_Returns_Null_When_User_Not_In_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var svc = new ProjectMemberReadService(repo);

            var (pId, _) = TestDataFactory.SeedUserWithProject(db);

            var role = await svc.GetRoleAsync(pId, Guid.NewGuid());
            role.Should().BeNull();
        }

        [Fact]
        public async Task CountActiveAsync_Returns_Expected_Values()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectMemberRepository(db);
            var svc = new ProjectMemberReadService(repo);

            var user = TestDataFactory.SeedUser(db);
            var count = await svc.CountActiveAsync(user.Id);
            count.Should().Be(0);

            TestDataFactory.SeedProject(db, user.Id);
            count = await svc.CountActiveAsync(user.Id);
            count.Should().Be(1);

            var otherUser = TestDataFactory.SeedUser(db);
            var secondProject = TestDataFactory.SeedProject(db, otherUser.Id); // new project with different owner
            count = await svc.CountActiveAsync(user.Id);
            count.Should().Be(1);

            TestDataFactory.SeedProjectMember(db, secondProject.Id, user.Id);
            count = await svc.CountActiveAsync(user.Id);
            count.Should().Be(2);

            secondProject.RemoveMember(user.Id, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();

            count = await svc.CountActiveAsync(user.Id);
            count.Should().Be(1);
        }
    }
}
