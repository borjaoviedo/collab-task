using Application.Common.Exceptions;
using Application.Projects.Services;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using TestHelpers.Common;
using TestHelpers.Common.Fakes;
using TestHelpers.Common.Testing;
using TestHelpers.Persistence;

namespace Application.Tests.Projects.Services
{
    [IntegrationTest]
    public sealed class ProjectReadServiceTests
    {
        [Fact]
        public async Task GetByIdAsync_Returns_Project_When_Exists_Otherwise_Null()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc) = await CreateSutAsync(dbh);

            var (projectId, _) = TestDataFactory.SeedUserWithProject(db);

            var existingResult = await readSvc.GetByIdAsync(projectId);
            existingResult.Should().NotBeNull();
            existingResult.Id.Should().Be(projectId);

            Func<Task> act = () => readSvc.GetByIdAsync(projectId: Guid.Empty);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task ListByUserAsync_Returns_All_Projects_By_User()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc) = await CreateSutAsync(dbh);

            var firstProjectName = "First";
            var secondProjectName = "Second";
            var thirdProjectName = "Third";

            var (_, firstUserId) = TestDataFactory.SeedUserWithProject(
                db,
                userName: firstProjectName);

            var firstUserList = await readSvc.ListByUserIdAsync(firstUserId);
            firstUserList.Should().NotBeNull();
            firstUserList.Should().HaveCount(1);

            var (_, secondUserId) = TestDataFactory.SeedUserWithProject(
                db,
                userName: secondProjectName);

            var secondUserList = await readSvc.ListByUserIdAsync(secondUserId);
            secondUserList.Should().NotBeNull();
            secondUserList.Should().HaveCount(1);

            TestDataFactory.SeedProject(db, firstUserId, thirdProjectName);

            firstUserList = await readSvc.ListByUserIdAsync(firstUserId);
            firstUserList.Should().HaveCount(2);

            secondUserList = await readSvc.ListByUserIdAsync(secondUserId);
            secondUserList.Should().HaveCount(1);
        }

        [Fact]
        public async Task ListByUserIdAsync_Returns_Empty_List_When_Not_Found_User()
        {
            using var dbh = new SqliteTestDb();
            var (_, readSvc) = await CreateSutAsync(dbh);

            var list = await readSvc.ListByUserIdAsync(userId: Guid.NewGuid());
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByUserIdAsync_Returns_Empty_List_When_User_Have_No_Projects()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc) = await CreateSutAsync(dbh);

            var user = TestDataFactory.SeedUser(db);
            var list = await readSvc.ListByUserIdAsync(user.Id);
            list.Should().BeEmpty();
        }

        // ---------- HELPERS ----------

        private static Task<(CollabTaskDbContext Db, ProjectReadService Service)>
            CreateSutAsync(
                SqliteTestDb dbh,
                Guid? userId = null)
        {
            var db = dbh.CreateContext();
            var repo = new ProjectRepository(db);

            // Ensure we always have a non-null current user id
            var currentUser = new FakeCurrentUserService
            {
                UserId = userId ?? Guid.NewGuid()
            };

            var svc = new ProjectReadService(
                repo,
                currentUser);

            return Task.FromResult((db, svc));
        }
    }
}
