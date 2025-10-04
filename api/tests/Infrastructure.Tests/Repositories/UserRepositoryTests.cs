using Application.Common.Abstractions.Persistence;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Repositories
{
    [Collection("SqlServerContainer")]
    public sealed class UserRepositoryTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;
        public UserRepositoryTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

        private (AppDbContext db, IUnitOfWork uow, UserRepository repo) BuildSut(string name)
        {
            var cs = $"{_baseCs};Database={name}";
            var sc = new ServiceCollection();
            sc.AddInfrastructure(cs);

            var sp = sc.BuildServiceProvider();
            var db = sp.GetRequiredService<AppDbContext>();
            db.Database.Migrate();

            var uow = sp.GetRequiredService<IUnitOfWork>();
            var repo = new UserRepository(db);
            return (db, uow, repo);
        }

        private static User NewUser(string email, string name, UserRole role = UserRole.User)
            => User.Create(Email.Create(email), UserName.Create(name), [1, 2, 3], [7, 8, 9], role);
        private static async Task<(User user, byte[] rowVersion)> InsertAsync(AppDbContext db, IUnitOfWork uow, string email, string name = "User Name", UserRole role = UserRole.User)
        {
            var u = NewUser(email, name, role);
            db.Users.Add(u);
            await uow.SaveChangesAsync();
            var rv = (byte[])db.Entry(u).Property(nameof(User.RowVersion)).CurrentValue!;
            return (u, rv);
        }

        [Fact]
        public async Task CreateAsync_Persists_User()
        {
            var (db, uow, repo) = BuildSut($"ct_{Guid.NewGuid():N}");

            var u = NewUser("a@b.com", "User Name");
            var id = await repo.CreateAsync(u);
            var changes = await uow.SaveChangesAsync();

            changes.Should().Be(1);
            id.Should().Be(u.Id);

            var fromDb = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            fromDb.Should().NotBeNull();
            fromDb!.Email.Should().Be(Email.Create("a@b.com"));
        }

        [Fact]
        public async Task GetByEmailAsync_Returns_User_When_Exists()
        {
            var (db, uow, repo) = BuildSut($"ct_{Guid.NewGuid():N}");
            var (u, _) = await InsertAsync(db, uow, "user1@example.com");

            var found = await repo.GetByEmailAsync("user1@example.com");

            found.Should().NotBeNull();
            found!.Id.Should().Be(u.Id);
        }

        [Fact]
        public async Task GetByEmailAsync_Returns_Null_When_Not_Found()
        {
            var (_, _, repo) = BuildSut($"ct_{Guid.NewGuid():N}");

            var found = await repo.GetByEmailAsync("missing@example.com");

            found.Should().BeNull();
        }

        [Fact]
        public async Task GetByNameAsync_Returns_User_When_Exists()
        {
            var (db, uow, repo) = BuildSut($"ct_{Guid.NewGuid():N}");
            var (u, _) = await InsertAsync(db, uow, "user1@example.com", name: "User Name");

            var found = await repo.GetByNameAsync("User Name");

            found.Should().NotBeNull();
            found!.Id.Should().Be(u.Id);
        }

        [Fact]
        public async Task GetByNameAsync_Returns_Null_When_Not_Found()
        {
            var (_, _, repo) = BuildSut($"ct_{Guid.NewGuid():N}");

            var found = await repo.GetByNameAsync("Not Found User Name");

            found.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_Returns_User_When_Exists_Null_Otherwise()
        {
            var (db, uow, repo) = BuildSut($"ct_{Guid.NewGuid():N}");
            var (u, _) = await InsertAsync(db, uow, "user2@example.com");

            var found = await repo.GetByIdAsync(u.Id);

            found.Should().NotBeNull();
            found!.Email.Value.Should().Be("user2@example.com");

            var notFound = await repo.GetByIdAsync(Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task ExistsByEmailAsync_True_When_Exists_False_Otherwise()
        {
            var (db, uow, repo) = BuildSut($"ct_{Guid.NewGuid():N}");
            await InsertAsync(db, uow, "user3@example.com");

            (await repo.ExistsByEmailAsync("user3@example.com")).Should().BeTrue();
            (await repo.ExistsByEmailAsync("nope@example.com")).Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByNameAsync_True_When_Exists_False_Otherwise()
        {
            var (db, uow, repo) = BuildSut($"ct_{Guid.NewGuid():N}");
            await InsertAsync(db, uow, "user3@example.com", name: "User Name");

            (await repo.ExistsByNameAsync("User Name")).Should().BeTrue();
            (await repo.ExistsByNameAsync("Diff User Name")).Should().BeFalse();
        }

        [Fact]
        public async Task AnyAdmin_And_CountAdmins_Work_As_Expected()
        {
            var (db, uow, repo) = BuildSut($"ct_{Guid.NewGuid():N}");
            (await repo.AnyAdminAsync()).Should().BeFalse();
            (await repo.CountAdminsAsync()).Should().Be(0);

            await InsertAsync(db, uow, "admin1@ex.com", "Admin Name", UserRole.Admin);
            (await repo.AnyAdminAsync()).Should().BeTrue();
            (await repo.CountAdminsAsync()).Should().Be(1);

            await InsertAsync(db, uow, "user4@ex.com", role: UserRole.User);
            (await repo.CountAdminsAsync()).Should().Be(1);
        }

        [Fact]
        public async Task SetRoleAsync_Updates_Role_When_RowVersion_Matches()
        {
            var (db, uow, repo) = BuildSut($"ct_{Guid.NewGuid():N}");
            var (u, rv) = await InsertAsync(db, uow, "user5@ex.com", role: UserRole.User);

            var updated = await repo.SetRoleAsync(u.Id, UserRole.Admin, rv);
            await uow.SaveChangesAsync();

            updated.Should().NotBeNull();
            updated!.Role.Should().Be(UserRole.Admin);
        }

        [Fact]
        public async Task SetRoleAsync_Returns_Null_On_Concurrency_Mismatch()
        {
            var (db, uow, repo) = BuildSut($"ct_{Guid.NewGuid():N}");
            var (u, _) = await InsertAsync(db, uow, "user6@ex.com", role: UserRole.User);

            var updated = await repo.SetRoleAsync(u.Id, UserRole.Admin, [1, 2, 3, 4]);

            updated.Should().BeNull();
        }

        [Fact]
        public async Task SetRoleAsync_NoOp_When_Role_Already_Set()
        {
            var (db, uow, repo) = BuildSut($"ct_{Guid.NewGuid():N}");
            var (u, rv) = await InsertAsync(db, uow, "user7@ex.com", role: UserRole.Admin);

            var updated = await repo.SetRoleAsync(u.Id, UserRole.Admin, rv);

            updated.Should().BeNull();

            var changes = await uow.SaveChangesAsync();
            changes.Should().Be(0);
        }

        [Fact]
        public async Task DeleteAsync_Removes_When_RowVersion_Matches()
        {
            var (db, uow, repo) = BuildSut($"ct_{Guid.NewGuid():N}");
            var (u, rv) = await InsertAsync(db, uow, "user8@ex.com");

            var ok = await repo.DeleteAsync(u.Id, rv);
            ok.Should().BeTrue();

            await uow.SaveChangesAsync();

            var exists = await db.Users.AsNoTracking().AnyAsync(x => x.Id == u.Id);
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_Returns_False_When_Id_Not_Found()
        {
            var (_, _, repo) = BuildSut($"ct_{Guid.NewGuid():N}");

            var ok = await repo.DeleteAsync(Guid.NewGuid(), [9, 9, 9]);

            ok.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_Returns_False_On_RowVersion_Mismatch()
        {
            var (db, uow, repo) = BuildSut($"ct_{Guid.NewGuid():N}");
            var (u, _) = await InsertAsync(db, uow, "user9@ex.com");

            var ok = await repo.DeleteAsync(u.Id, [5, 5, 5]);

            ok.Should().BeFalse();
            (await uow.SaveChangesAsync()).Should().Be(0);
        }

        [Fact]
        public async Task Unique_Email_Is_Enforced()
        {
            var (db, uow, repo) = BuildSut($"ct_{Guid.NewGuid():N}");
            await InsertAsync(db, uow, "dup@ex.com");

            await repo.CreateAsync(NewUser("dup@ex.com", "Not Default User Name"));

            await FluentActions.Invoking(() => uow.SaveChangesAsync())
                .Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task Unique_Name_Is_Enforced()
        {
            var (db, uow, repo) = BuildSut($"ct_{Guid.NewGuid():N}");
            await InsertAsync(db, uow, "first@ex.com", name: "Dup Name");

            await repo.CreateAsync(NewUser("second@ex.com", "Dup Name"));

            await FluentActions.Invoking(() => uow.SaveChangesAsync())
                .Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task Email_Normalization_Allows_MixedCase_Lookup()
        {
            var (db, uow, repo) = BuildSut($"ct_{Guid.NewGuid():N}");
            var (u, _) = await InsertAsync(db, uow, "MiXeD@Example.COM");

            var found = await repo.GetByEmailAsync("mixed@example.com");

            found.Should().NotBeNull();
            found!.Id.Should().Be(u.Id);
        }

        [Fact]
        public async Task Name_Normalization_Allows_MixedCase_Lookup()
        {
            var (db, uow, repo) = BuildSut($"ct_{Guid.NewGuid():N}");
            var (u, _) = await InsertAsync(db, uow, "mixed@example.com", name: "MiXeD uSeR NaMe");

            var found = await repo.GetByNameAsync("mixed user name");

            found.Should().NotBeNull();
            found!.Id.Should().Be(u.Id);
        }
    }
}
