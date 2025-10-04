using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Persistence
{
    [Collection("SqlServerContainer")]
    public sealed class UserPersistenceContractTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;
        public UserPersistenceContractTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

        private (ServiceProvider sp, AppDbContext db) BuildDb(string name)
        {
            var cs = $"{_baseCs};Database={name}";
            var sc = new ServiceCollection();
            sc.AddInfrastructure(cs);
            var sp = sc.BuildServiceProvider();
            var db = sp.GetRequiredService<AppDbContext>();
            return (sp, db);
        }

        [Fact]
        public async Task Add_And_GetByEmail_Works()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var u = User.Create(Email.Create("repo@demo.com"), UserName.Create("Repo User"), [7], [9]);
            db.Users.Add(u);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var found = await db.Users.SingleOrDefaultAsync(x => x.Email == Email.Create("repo@demo.com"));
            found.Should().NotBeNull();
            found!.Id.Should().Be(u.Id);
        }

        [Fact]
        public async Task GetByName_Works()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var u = User.Create(Email.Create("repo@demo.com"), UserName.Create("Repo User"), [7], [9]);
            db.Users.Add(u);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var found = await db.Users.SingleOrDefaultAsync(x => x.Name == UserName.Create("Repo User"));
            found.Should().NotBeNull();
            found.Name.Should().Be(u.Name);
        }

        [Fact]
        public async Task Update_Role_With_Concurrency_Token()
        {
            var (sp, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var u = User.Create(Email.Create("concurrency@demo.com"), UserName.Create("Concurrency User"), [1], [1]);
            db.Users.Add(u);
            await db.SaveChangesAsync();

            // simulate stale rowversion
            var stale = u.RowVersion!.ToArray();

            // first update succeeds
            u.Role = UserRole.Admin;
            await db.SaveChangesAsync();

            // second context tries with stale rowversion
            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.Users.SingleAsync(x => x.Id == u.Id);

            // concurrence simulation
            var entry = db2.Entry(same);
            entry.Property(x => x.RowVersion).OriginalValue = stale;

            same.Role = UserRole.User;

            Func<Task> act = async () => await db2.SaveChangesAsync();
            await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        }
    }
}
