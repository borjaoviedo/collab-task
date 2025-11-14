using Application.Projects.Services;
using FluentAssertions;
using Infrastructure.Persistence.Repositories;
using TestHelpers.Common;
using TestHelpers.Persistence;

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
            var readSvc = new ProjectReadService(repo);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);

            var existingResult = await readSvc.GetAsync(projectId);
            existingResult.Should().NotBeNull();

            var notFoundResult = await readSvc.GetAsync(projectId: Guid.Empty);
            notFoundResult.Should().BeNull();
        }

        [Fact]
        public async Task ListByUserAsync_Returns_All_Projects_By_User()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);
            var readSvc = new ProjectReadService(repo);

            var firstProjectName = "First";
            var secondProjectName = "Second";
            var thirdProjectName = "Third";
            var (_, firstUserId) = TestDataFactory.SeedUserWithProject(
                db,
                userName: firstProjectName);

            var firstUserList = await readSvc.ListByUserAsync(firstUserId);
            firstUserList.Should().NotBeNull();
            firstUserList.Should().HaveCount(1);
            var (_, secondUserId) = TestDataFactory.SeedUserWithProject(
                db,
                userName: secondProjectName);

            var secondUserList = await readSvc.ListByUserAsync(secondUserId);
            secondUserList.Should().NotBeNull();
            secondUserList.Should().HaveCount(1);

            TestDataFactory.SeedProject(db, firstUserId, thirdProjectName);
            firstUserList = await readSvc.ListByUserAsync(firstUserId);
            firstUserList.Should().HaveCount(2);

            secondUserList = await readSvc.ListByUserAsync(secondUserId);
            secondUserList.Should().HaveCount(1);
        }

        [Fact]
        public async Task ListByUserAsync_Returns_Empty_List_When_Not_Found_User()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);
            var readSvc = new ProjectReadService(repo);

            var list = await readSvc.ListByUserAsync(userId: Guid.NewGuid());
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByUserAsync_Returns_Empty_List_When_User_Have_No_Projects()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);
            var readSvc = new ProjectReadService(repo);

            var user = TestDataFactory.SeedUser(db);
            var list = await readSvc.ListByUserAsync(user.Id);
            list.Should().BeEmpty();
        }
    }
}
