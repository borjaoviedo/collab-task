using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers;

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
            var u = User.Create(email, UserName.Create("User Name"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));

            await repo.AddAsync(u);
            await uow.SaveAsync(MutationKind.Create);

            var id = u.Id;
            id.Should().Be(u.Id);

            var fromDb = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
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
            var u = TestDataFactory.SeedUser(db, email);

            var found = await repo.GetByEmailAsync(email);

            found.Should().NotBeNull();
            found!.Id.Should().Be(u.Id);
        }

        [Fact]
        public async Task GetByEmailAsync_Returns_Null_When_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            var found = await repo.GetByEmailAsync(Email.Create("missing@example.com"));

            found.Should().BeNull();
        }

        [Fact]
        public async Task GetTrackedByIdAsync_Returns_User_When_Exists_Null_Otherwise()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            var u = TestDataFactory.SeedUser(db);
            var found = await repo.GetTrackedByIdAsync(u.Id);

            found.Should().NotBeNull();
            found.Id.Should().Be(u.Id);

            var notFound = await repo.GetTrackedByIdAsync(Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_Returns_All_Users_List()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            TestDataFactory.SeedUser(db);
            var list = await repo.GetAllAsync();
            list.Should().NotBeNull();
            list.Count.Should().Be(1);

            TestDataFactory.SeedUser(db);
            list = await repo.GetAllAsync();
            list.Count.Should().Be(2);

            TestDataFactory.SeedUser(db);
            list = await repo.GetAllAsync();
            list.Count.Should().Be(3);
        }

        [Fact]
        public async Task GetAllAsync_Returns_Empty_List_When_No_Users()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            var list = await repo.GetAllAsync();
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

            (await repo.ExistsWithEmailAsync(email)).Should().BeTrue();
            (await repo.ExistsWithEmailAsync(Email.Create("nope@example.com"))).Should().BeFalse();
        }

        [Fact]
        public async Task ExistsWithNameAsync_True_When_Exists_False_Otherwise()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            var name = UserName.Create("Name");
            TestDataFactory.SeedUser(db, name: name);

            (await repo.ExistsWithNameAsync(name)).Should().BeTrue();
            (await repo.ExistsWithNameAsync(UserName.Create("Diff User Name"))).Should().BeFalse();
        }

        [Fact]
        public async Task RenameAsync_NoOp_When_Name_Unchanged()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            var name = UserName.Create("Same");
            var u = TestDataFactory.SeedUser(db, name: name);

            var res = await repo.RenameAsync(u.Id, name, u.RowVersion);

            res.Should().Be(PrecheckStatus.NoOp);
        }

        [Fact]
        public async Task RenameAsync_Updated_When_Name_Changes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var uow = new UnitOfWork(db);

            var user = TestDataFactory.SeedUser(db);

            var res = await repo.RenameAsync(user.Id, UserName.Create("New"), user.RowVersion);
            res.Should().Be(PrecheckStatus.Ready);

            await uow.SaveAsync(MutationKind.Update);

            var fromDb = await db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
            fromDb.Name.Value.Should().Be("New");
        }

        [Fact]
        public async Task ChangeRoleAsync_Updates_Role_When_RowVersion_Matches()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var uow = new UnitOfWork(db);

            var user = TestDataFactory.SeedUser(db);
            var res = await repo.ChangeRoleAsync(user.Id, UserRole.Admin, user.RowVersion);

            res.Should().Be(PrecheckStatus.Ready);

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

            var u = TestDataFactory.SeedUser(db, role: UserRole.Admin);

            var res = await repo.ChangeRoleAsync(u.Id, UserRole.Admin, u.RowVersion);
            res.Should().Be(PrecheckStatus.NoOp);
        }

        [Fact]
        public async Task DeleteAsync_Removes_When_RowVersion_Matches()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var uow = new UnitOfWork(db);

            var user = TestDataFactory.SeedUser(db);

            var res = await repo.DeleteAsync(user.Id, user.RowVersion);
            res.Should().Be(PrecheckStatus.Ready);

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

            var res = await repo.DeleteAsync(Guid.NewGuid(), [9, 9, 9]);
            res.Should().Be(PrecheckStatus.NotFound);
        }

        [Fact]
        public async Task Unique_Email_Is_Enforced()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var sameEmail = "same@e.com";
            TestDataFactory.SeedUser(db, sameEmail);

            FluentActions.Invoking(() => TestDataFactory.SeedUser(db, sameEmail))
                .Should().Throw<DbUpdateException>();
        }

        [Fact]
        public async Task Unique_Name_Is_Enforced()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var sameName = "Same name";
            TestDataFactory.SeedUser(db, name: sameName);

            FluentActions.Invoking(() => TestDataFactory.SeedUser(db, name: sameName))
                .Should().Throw<DbUpdateException>();
        }

        [Fact]
        public async Task Email_Normalization_Allows_MixedCase_Lookup()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            var u = TestDataFactory.SeedUser(db, "SaMe@e.CoM");

            var found = await repo.GetByEmailAsync(Email.Create("same@e.com"));

            found.Should().NotBeNull();
            found!.Id.Should().Be(u.Id);
        }
    }
}
