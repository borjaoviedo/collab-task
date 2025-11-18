using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Common;
using TestHelpers.Common.Testing;
using TestHelpers.Persistence;

namespace Infrastructure.Tests.Repositories
{
    [IntegrationTest]
    public sealed class UserRepositoryTests
    {
        // --------------- GetByIdAsync ---------------

        [Fact]
        public async Task GetByIdAsync_Returns_User_When_Exists()
        {
            using var dbh = new SqliteTestDb();
            var (db, repo) = await CreateSutAsync(dbh);

            var user = TestDataFactory.SeedUser(db);

            var found = await repo.GetByIdAsync(user.Id);

            found.Should().NotBeNull();
            found!.Id.Should().Be(user.Id);
        }

        [Fact]
        public async Task GetByIdAsync_Returns_Null_When_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            var (_, repo) = await CreateSutAsync(dbh);

            var found = await repo.GetByIdAsync(Guid.NewGuid());

            found.Should().BeNull();
        }

        // --------------- GetByEmailAsync ---------------

        [Fact]
        public async Task GetByEmailAsync_Returns_User_When_Exists()
        {
            using var dbh = new SqliteTestDb();
            var (db, repo) = await CreateSutAsync(dbh);

            const string rawEmail = "user1@demo.com";

            var user = TestDataFactory.SeedUser(db, email: rawEmail);

            var emailVo = Email.Create(rawEmail);
            var found = await repo.GetByEmailAsync(emailVo);

            found.Should().NotBeNull();
            found!.Id.Should().Be(user.Id);
            found.Email.Value.Should().Be(rawEmail);
        }

        [Fact]
        public async Task GetByEmailAsync_Returns_Null_When_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            var (_, repo) = await CreateSutAsync(dbh);

            var normalized = Email.Create("missing@demo.com");

            var found = await repo.GetByEmailAsync(normalized);

            found.Should().BeNull();
        }

        // --------------- Add / Update / Remove ---------------

        [Fact]
        public async Task AddAsync_Persists_User_After_SaveChanges()
        {
            using var dbh = new SqliteTestDb();
            var (db, repo) = await CreateSutAsync(dbh);

            var user = User.Create(
                Email.Create("new@demo.com"),
                UserName.Create("New User"),
                TestDataFactory.CreateHash(),
                TestDataFactory.CreateSalt(),
                UserRole.User);

            await repo.AddAsync(user);
            await db.SaveChangesAsync();

            var reloaded = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            reloaded.Should().NotBeNull();
            reloaded!.Email.Value.Should().Be("new@demo.com");
        }

        [Fact]
        public async Task UpdateAsync_Marks_Entity_Modified_And_Persists_Changes()
        {
            using var dbh = new SqliteTestDb();
            var (db, repo) = await CreateSutAsync(dbh);

            var user = TestDataFactory.SeedUser(db, name: "Old Name");

            // Modify through domain behavior
            user.Rename(UserName.Create("Updated Name"));

            await repo.UpdateAsync(user);
            await db.SaveChangesAsync();

            var reloaded = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            reloaded.Should().NotBeNull();
            reloaded!.Name.Value.Should().Be("Updated Name");
        }

        [Fact]
        public async Task RemoveAsync_Deletes_User_On_SaveChanges()
        {
            using var dbh = new SqliteTestDb();
            var (db, repo) = await CreateSutAsync(dbh);

            var user = TestDataFactory.SeedUser(db);

            await repo.RemoveAsync(user);
            await db.SaveChangesAsync();

            var reloaded = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == user.Id);

            reloaded.Should().BeNull();
        }

        // ---------- HELPERS ----------

        private static Task<(CollabTaskDbContext Db, UserRepository Repo)>
            CreateSutAsync(SqliteTestDb dbh)
        {
            var db = dbh.CreateContext();
            var repo = new UserRepository(db);

            return Task.FromResult((db, repo));
        }
    }
}
