using Application.Users.Services;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers;

namespace Application.Tests.Users.Services
{
    public sealed class UserWriteServiceTests
    {
        private readonly byte[] _validHash = TestDataFactory.Bytes(32);
        private readonly byte[] _validSalt = TestDataFactory.Bytes(16);

        [Fact]
        public async Task CreateAsync_Returns_Created_And_User()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new UserRepository(db);
            var svc = new UserWriteService(repo);

            var (created, user) = await svc.CreateAsync(
                Email.Create("email@e.com"),
                UserName.Create("user name"),
                _validHash,
                _validSalt,
                UserRole.Admin);

            created.Should().Be(DomainMutation.Created);
            user.Should().NotBeNull();
        }

        [Fact]
        public async Task Rename_Delegates_To_Repository()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new UserRepository(db);
            var svc = new UserWriteService(repo);

            var user = TestDataFactory.SeedUser(db);

            var current = await db.Users.FirstAsync(u => u.Id == user!.Id);
            var result = await svc.RenameAsync(user!.Id, UserName.Create("new name"), current.RowVersion);
            result.Should().Be(DomainMutation.Updated);
        }

        [Fact]
        public async Task RenameAsync_Returns_NoOp_When_Unchanged()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new UserRepository(db);
            var svc = new UserWriteService(repo);

            var sameName = UserName.Create("same name");
            var u = TestDataFactory.SeedUser(db, name: sameName);

            var res = await svc.RenameAsync(u!.Id, sameName, u.RowVersion);
            res.Should().Be(DomainMutation.NoOp);
        }

        [Fact]
        public async Task ChangeRoleAsync_Returns_Conflict_When_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new UserRepository(db);
            var svc = new UserWriteService(repo);

            var u = TestDataFactory.SeedUser(db);

            var res = await svc.ChangeRoleAsync(u!.Id, UserRole.Admin, [1, 2, 3, 4]);
            res.Should().Be(DomainMutation.Conflict);
        }

        [Fact]
        public async Task Delete_Delegates_To_Repository()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new UserRepository(db);
            var svc = new UserWriteService(repo);

            var user = TestDataFactory.SeedUser(db);

            var current = await db.Users.FirstAsync(x => x.Id == user!.Id);
            var del = await svc.DeleteAsync(user!.Id, current.RowVersion);
            del.Should().Be(DomainMutation.Deleted);
        }
    }
}
