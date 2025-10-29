using Application.Users.Services;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Common;
using TestHelpers.Persistence;

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
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var uow = new UnitOfWork(db);
            var writeSvc = new UserWriteService(repo, uow);

            var (created, user) = await writeSvc.CreateAsync(
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
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var uow = new UnitOfWork(db);
            var writeSvc = new UserWriteService(repo, uow);

            var user = TestDataFactory.SeedUser(db);
            var current = await db.Users.FirstAsync(u => u.Id == user.Id);
            var result = await writeSvc.RenameAsync(
                user.Id,
                UserName.Create("new name"),
                current.RowVersion);

            result.Should().Be(DomainMutation.Updated);
        }

        [Fact]
        public async Task RenameAsync_Returns_NoOp_When_Unchanged()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var uow = new UnitOfWork(db);
            var writeSvc = new UserWriteService(repo, uow);

            var sameName = UserName.Create("same name");
            var user = TestDataFactory.SeedUser(db, name: sameName);
            var result = await writeSvc.RenameAsync(user.Id, sameName, user.RowVersion);

            result.Should().Be(DomainMutation.NoOp);
        }

        [Fact]
        public async Task ChangeRoleAsync_Returns_Conflict_When_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var uow = new UnitOfWork(db);
            var writeSvc = new UserWriteService(repo, uow);

            var user = TestDataFactory.SeedUser(db);
            var result = await writeSvc.ChangeRoleAsync(
                user.Id,
                UserRole.Admin,
                rowVersion: [1, 2, 3, 4]);

            result.Should().Be(DomainMutation.Conflict);
        }

        [Fact]
        public async Task Delete_Delegates_To_Repository()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var uow = new UnitOfWork(db);
            var writeSvc = new UserWriteService(repo, uow);

            var user = TestDataFactory.SeedUser(db);
            var current = await db.Users.FirstAsync(u => u.Id == user.Id);
            var result = await writeSvc.DeleteAsync(user.Id, current.RowVersion);

            result.Should().Be(DomainMutation.Deleted);
        }
    }
}
