using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Common;
using TestHelpers.Persistence;

namespace Infrastructure.Tests.Repositories
{
    public sealed class UserRepositoryTests
    {
        [Fact]
        public async Task AddAsync_Persists_User()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var uow = new UnitOfWork(db);

            var email = Email.Create("a@b.com");
            var user = User.Create(
                email,
                UserName.Create("User Name"),
                TestDataFactory.Bytes(32),
                TestDataFactory.Bytes(16));

            await repo.AddAsync(user);
            await uow.SaveAsync(MutationKind.Create);

            var userId = user.Id;
            userId.Should().Be(user.Id);

            var fromDb = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.Id == userId);
            fromDb.Should().NotBeNull();
            fromDb!.Email.Should().Be(email);
        }

        [Fact]
        public async Task GetByEmailAsync_Returns_User_When_Exists()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            var email = Email.Create("user1@example.com");
            var user = TestDataFactory.SeedUser(db, email);

            var found = await repo.GetByEmailAsync(email);

            found.Should().NotBeNull();
            found.Id.Should().Be(user.Id);
        }

        [Fact]
        public async Task GetByEmailAsync_Returns_Null_When_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            var found = await repo.GetByEmailAsync(Email.Create("missing@e.com"));

            found.Should().BeNull();
        }

        [Fact]
        public async Task GetTrackedByIdAsync_Returns_User_When_Exists_Null_Otherwise()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            var user = TestDataFactory.SeedUser(db);
            var found = await repo.GetTrackedByIdAsync(user.Id);

            found.Should().NotBeNull();
            found.Id.Should().Be(user.Id);

            var notFound = await repo.GetTrackedByIdAsync(id: Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task ListAsync_Returns_All_Users_List()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            TestDataFactory.SeedUser(db);
            var list = await repo.ListAsync();
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            TestDataFactory.SeedUser(db);
            list = await repo.ListAsync();
            list.Count.Should().Be(2);

            TestDataFactory.SeedUser(db);
            list = await repo.ListAsync();
            list.Count.Should().Be(3);
        }

        [Fact]
        public async Task ListAsync_Returns_Empty_List_When_No_Users()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            var list = await repo.ListAsync();
            list.Should().BeEmpty();
        }

        [Fact]
        public async Task ExistsWithEmailAsync_True_When_Exists_False_Otherwise()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            var email = Email.Create("existing@e.com");
            TestDataFactory.SeedUser(db, email);

            var existsWithExistingEmail = await repo.ExistsWithEmailAsync(email);
            existsWithExistingEmail.Should().BeTrue();

            var differentEmail = Email.Create("different@e.com");
            var exisitsWithNonExistingEmail = await repo.ExistsWithEmailAsync(differentEmail);
            exisitsWithNonExistingEmail.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsWithNameAsync_True_When_Exists_False_Otherwise()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            var name = UserName.Create("Name");
            TestDataFactory.SeedUser(db, name: name);

            var existsWithExistingName = await repo.ExistsWithNameAsync(name);
            existsWithExistingName.Should().BeTrue();

            var differentName = UserName.Create("Diff User Name");
            var existsWithNonExistingName = await repo.ExistsWithNameAsync(differentName);
            existsWithNonExistingName.Should().BeFalse();
        }

        [Fact]
        public async Task RenameAsync_NoOp_When_Name_Unchanged()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            var name = UserName.Create("Same");
            var user = TestDataFactory.SeedUser(db, name: name);

            var result = await repo.RenameAsync(user.Id, name, user.RowVersion);

            result.Should().Be(PrecheckStatus.NoOp);
        }

        [Fact]
        public async Task RenameAsync_Updated_When_Name_Changes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var uow = new UnitOfWork(db);

            var user = TestDataFactory.SeedUser(db);

            var userName = "user name";
            var result = await repo.RenameAsync(user.Id, UserName.Create(userName), user.RowVersion);
            result.Should().Be(PrecheckStatus.Ready);

            await uow.SaveAsync(MutationKind.Update);

            var fromDb = await db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
            fromDb.Name.Value.Should().Be(userName);
        }

        [Fact]
        public async Task ChangeRoleAsync_Updates_Role_When_RowVersion_Matches()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var uow = new UnitOfWork(db);

            var user = TestDataFactory.SeedUser(db);
            var result = await repo.ChangeRoleAsync(user.Id, UserRole.Admin, user.RowVersion);

            result.Should().Be(PrecheckStatus.Ready);

            await uow.SaveAsync(MutationKind.Update);

            var fromDb = await db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
            fromDb.Role.Should().Be(UserRole.Admin);
        }

        [Fact]
        public async Task ChangeRoleAsync_NoOp_When_Role_Already_Set()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            var user = TestDataFactory.SeedUser(db, role: UserRole.Admin);

            var result = await repo.ChangeRoleAsync(user.Id, UserRole.Admin, user.RowVersion);
            result.Should().Be(PrecheckStatus.NoOp);
        }

        [Fact]
        public async Task DeleteAsync_Removes_When_RowVersion_Matches()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var uow = new UnitOfWork(db);

            var user = TestDataFactory.SeedUser(db);

            var result = await repo.DeleteAsync(user.Id, user.RowVersion);
            result.Should().Be(PrecheckStatus.Ready);

            await uow.SaveAsync(MutationKind.Delete);

            var fromDb = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
            fromDb.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_Returns_NotFound_When_Id_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            var result = await repo.DeleteAsync(id: Guid.NewGuid(), rowVersion: [9, 9, 9]);
            result.Should().Be(PrecheckStatus.NotFound);
        }

        [Fact]
        public async Task Unique_Email_Is_Enforced()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var sameEmail = "same@e.com";
            TestDataFactory.SeedUser(db, sameEmail);

            FluentActions.Invoking(()
                => TestDataFactory.SeedUser(db, sameEmail)).Should().Throw<DbUpdateException>();
        }

        [Fact]
        public async Task Unique_Name_Is_Enforced()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var sameName = "Same name";
            TestDataFactory.SeedUser(db, name: sameName);

            FluentActions.Invoking(()
                => TestDataFactory.SeedUser(db, name: sameName)).Should().Throw<DbUpdateException>();
        }

        [Fact]
        public async Task Email_Normalization_Allows_MixedCase_Lookup()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            var user = TestDataFactory.SeedUser(db, "SaMe@e.CoM");
            var found = await repo.GetByEmailAsync(Email.Create("same@e.com"));

            found.Should().NotBeNull();
            found.Id.Should().Be(user.Id);
        }
    }
}
