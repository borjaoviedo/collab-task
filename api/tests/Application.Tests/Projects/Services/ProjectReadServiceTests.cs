using Application.Projects.Services;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using TestHelpers;

namespace Application.Tests.Projects.Services
{
    public sealed class ProjectReadServiceTests
    {
        [Fact]
        public async Task GetAsync_Returns_Project_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);
            var svc = new ProjectReadService(repo);

            var (pId, _) = TestDataFactory.SeedUserWithProject(db);

            var existingResult = await svc.GetAsync(pId);
            existingResult.Should().NotBeNull();

            var notFoundResult = await svc.GetAsync(Guid.Empty);
            notFoundResult.Should().BeNull();
        }

        [Fact]
        public async Task ListByUserAsync_Returns_All_Projects_By_User()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);
            var svc = new ProjectReadService(repo);

            var firstProjectName = "First";
            var secondProjectName = "Second";
            var (_, firstUserId) = TestDataFactory.SeedUserWithProject(db, projectName: firstProjectName);

            var firstUserList = await svc.ListByUserAsync(firstUserId);
            firstUserList.Should().NotBeNull();
            firstUserList.Should().HaveCount(1);
            var (_, secondUserId) = TestDataFactory.SeedUserWithProject(db, projectName: secondProjectName);

            var secondUserList = await svc.ListByUserAsync(secondUserId);
            secondUserList.Should().NotBeNull();
            secondUserList.Should().HaveCount(1);

            var thirdProjectName = "Third";
            TestDataFactory.SeedProject(db, firstUserId, thirdProjectName);
            firstUserList = await svc.ListByUserAsync(firstUserId);
            firstUserList.Should().HaveCount(2);

            secondUserList = await svc.ListByUserAsync(secondUserId);
            secondUserList.Should().HaveCount(1);
        }

        [Fact]
        public async Task ListByUserAsync_Returns_Empty_List_When_Not_Found_User()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);
            var svc = new ProjectReadService(repo);

            var list = await svc.ListByUserAsync(Guid.NewGuid());
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByUserAsync_Returns_Empty_List_When_User_Have_No_Projects()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);
            var svc = new ProjectReadService(repo);

            var user = TestDataFactory.SeedUser(db);
            var list = await svc.ListByUserAsync(user.Id);
            list.Should().BeEmpty();
        }
    }
}
