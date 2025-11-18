using Application.Common.Exceptions;
using Application.Users.DTOs;
using Application.Users.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Common;
using TestHelpers.Common.Fakes;
using TestHelpers.Common.Testing;
using TestHelpers.Persistence;

namespace Application.Tests.Users.Services
{
    [IntegrationTest]
    public sealed class UserWriteServiceTests
    {

        // --------------- ChangeRoleAsync ---------------

        [Fact]
        public async Task ChangeRoleAsync_Updates_Role()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, _) = await CreateSutAsync(dbh);

            var user = TestDataFactory.SeedUser(db, role: UserRole.User);
            var dto = new UserChangeRoleDto { NewRole = UserRole.Admin };

            var result = await writeSvc.ChangeRoleAsync(user.Id, dto);

            result.Role.Should().Be(UserRole.Admin);

            var reloaded = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
            reloaded!.Role.Should().Be(UserRole.Admin);
        }

        [Fact]
        public async Task ChangeRoleAsync_Throws_When_User_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            var (_, writeSvc, _) = await CreateSutAsync(dbh);

            var dto = new UserChangeRoleDto { NewRole = UserRole.Admin };

            await FluentActions.Invoking(() =>
                writeSvc.ChangeRoleAsync(Guid.NewGuid(), dto))
                .Should()
                .ThrowAsync<NotFoundException>();
        }

        // --------------- RenameAsync ---------------

        [Fact]
        public async Task RenameAsync_Throws_When_Not_Authenticated()
        {
            using var dbh = new SqliteTestDb();
            var (_, writeSvc, _) = await CreateSutAsync(dbh);

            var dto = new UserRenameDto { NewName = "New Name" };

            await FluentActions.Invoking(() =>
                writeSvc.RenameAsync(dto))
                .Should()
                .ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task RenameAsync_Updates_Current_User_Name()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, currentUser) = await CreateSutAsync(dbh);

            var user = TestDataFactory.SeedUser(db);
            currentUser.UserId = user.Id;

            var dto = new UserRenameDto { NewName = "John Doe" };

            var result = await writeSvc.RenameAsync(dto);

            result.Name.Should().Be("John Doe");

            var reloaded = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
            reloaded!.Name.Value.Should().Be("John Doe");
        }
        
        // --------------- DeleteById ---------------

        [Fact]
        public async Task DeleteByIdAsync_Is_NoOp_When_User_Not_Found()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, _) = await CreateSutAsync(dbh);

            var existing = TestDataFactory.SeedUser(db);

            await writeSvc.DeleteByIdAsync(Guid.NewGuid());

            var users = await db.Users.AsNoTracking().ToListAsync();
            users.Should().ContainSingle(u => u.Id == existing.Id);
        }


        // ---------- HELPERS ----------

        private static Task<(CollabTaskDbContext Db, UserWriteService Service, FakeCurrentUserService CurrentUser)>
            CreateSutAsync(
                SqliteTestDb dbh,
                Guid? userId = null)
        {
            var db = dbh.CreateContext();
            var repo = new UserRepository(db);
            var uow = new UnitOfWork(db);
            var currentUser = new FakeCurrentUserService
            {
                UserId = userId
            };
            var passwordHasher = new FakePasswordHasher();
            var jwtTokenService = new FakeJwtTokenService();

            var svc = new UserWriteService(repo, uow, currentUser, passwordHasher, jwtTokenService);
            return Task.FromResult((db, svc, currentUser));
        }
    }
}
