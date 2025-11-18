using Application.Common.Exceptions;
using Application.Users.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using TestHelpers.Common;
using TestHelpers.Common.Fakes;
using TestHelpers.Common.Testing;
using TestHelpers.Persistence;

namespace Application.Tests.Users.Services
{
    [IntegrationTest]
    public sealed class UserReadServiceTests
    {
        // --------------- GetByIdAsync ---------------

        [Fact]
        public async Task GetByIdAsync_Returns_Entity()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc, _) = await CreateSutAsync(dbh);

            var user = TestDataFactory.SeedUser(db);
            var dto = await readSvc.GetByIdAsync(user.Id);

            dto.Should().NotBeNull();
            dto!.Id.Should().Be(user.Id);
        }

        [Fact]
        public async Task GetByIdAsync_Throws_When_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            var (_, readSvc, _) = await CreateSutAsync(dbh);

            await FluentActions.Invoking(() =>
                readSvc.GetByIdAsync(Guid.NewGuid()))
                .Should()
                .ThrowAsync<NotFoundException>();
        }

        // --------------- GetCurrentAsync ---------------

        [Fact]
        public async Task GetCurrentAsync_Returns_Current_User()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc, currentUser) = await CreateSutAsync(
                dbh,
                userId: null);

            var user = TestDataFactory.SeedUser(db, role: UserRole.Admin);
            currentUser.UserId = user.Id;

            var dto = await readSvc.GetCurrentAsync();

            dto.Should().NotBeNull();
            dto!.Id.Should().Be(user.Id);
        }

        // --------------- SearchAsync ---------------

        [Fact]
        public async Task SearchAsync_Returns_Users_List()
        {
            using var dbh = new SqliteTestDb();
            var (db, readSvc, _) = await CreateSutAsync(dbh);

            // Seed users with different roles
            TestDataFactory.SeedUser(db, role: UserRole.User);
            TestDataFactory.SeedUser(db, role: UserRole.Admin);

            var result = await readSvc.ListAsync();

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListAsync_Returns_Empty_When_No_Matches()
        {
            using var dbh = new SqliteTestDb();
            var (_, readSvc, _) = await CreateSutAsync(dbh);

            var result = await readSvc.ListAsync();

            result.Should().BeEmpty();
        }

        // ---------- HELPERS ----------

        private static Task<(CollabTaskDbContext Db, UserReadService Service, FakeCurrentUserService CurrentUser)>
            CreateSutAsync(
                SqliteTestDb dbh,
                Guid? userId = null)
        {
            var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var currentUser = new FakeCurrentUserService
            {
                UserId = userId
            };
            var service = new UserReadService(repo, currentUser);

            return Task.FromResult((db, service, currentUser));
        }
    }
}
