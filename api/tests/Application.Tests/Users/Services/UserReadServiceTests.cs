using Application.Users.Services;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using TestHelpers;

namespace Application.Tests.Users.Services
{
    public sealed class UserReadServiceTests
    {
        [Fact]
        public async Task Get_Returns_Entity()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var svc = new UserReadService(repo);

            var user = TestDataFactory.SeedUser(db);
            var found = await svc.GetAsync(user.Id);

            found.Should().NotBeNull();
        }

        [Fact]
        public async Task Get_Returns_Null_When_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var svc = new UserReadService(repo);

            var found = await svc.GetAsync(Guid.Empty);
            found.Should().BeNull();
        }

        [Fact]
        public async Task GetByEmail_Returns_Entity()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var svc = new UserReadService(repo);

            var user = TestDataFactory.SeedUser(db);
            var found = await svc.GetByEmailAsync(user.Email);

            found.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByEmail_Returns_Null_When_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var svc = new UserReadService(repo);

            var found = await svc.GetByEmailAsync(Email.Create("email@e.com"));
            found.Should().BeNull();
        }

        [Fact]
        public async Task List_Returns_Users()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var svc = new UserReadService(repo);

            var firstUserName = "User A";
            var secondUserName = "User B";
            TestDataFactory.SeedUser(db, name: firstUserName);
            TestDataFactory.SeedUser(db, name: secondUserName);

            var list = await svc.ListAsync();
            list.Select(x => x.Name.Value).Should().ContainInOrder(firstUserName, secondUserName);
        }

        [Fact]
        public async Task List_Returns_Empty_When_None()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var svc = new UserReadService(repo);

            var list = await svc.ListAsync();
            list.Should().BeEmpty();
        }
    }
}
