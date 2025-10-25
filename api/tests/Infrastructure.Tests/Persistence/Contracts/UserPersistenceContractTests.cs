using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestHelpers;

namespace Infrastructure.Tests.Persistence.Contracts
{
    [Collection("SqlServerContainer")]
    public sealed class UserPersistenceContractTests(MsSqlContainerFixture fx)
    {
        private readonly MsSqlContainerFixture _fx = fx;
        private readonly string _cs = fx.ConnectionString;

        [Fact]
        public async Task Add_And_GetByEmail_Works()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var u = User.Create(Email.Create("repo@demo.com"), UserName.Create("Repo User"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
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
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var u = User.Create(Email.Create("repo@demo.com"), UserName.Create("Repo User"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            db.Users.Add(u);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var found = await db.Users.SingleOrDefaultAsync(x => x.Name == UserName.Create("Repo User"));

            found.Should().NotBeNull();
            found!.Name.Should().Be(u.Name);
        }

        [Fact]
        public async Task Unique_Index_On_Email()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var email = Email.Create("same@demo.com");
            var u1 = User.Create(email, UserName.Create("First"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            db.Users.Add(u1);
            await db.SaveChangesAsync();

            var u2 = User.Create(email, UserName.Create("Second"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            db.Users.Add(u2);

            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        [Fact]
        public async Task Unique_Index_On_Name()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var name = UserName.Create("Same name");
            var u1 = User.Create(Email.Create("u1@demo.com"), name, TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            db.Users.Add(u1);
            await db.SaveChangesAsync();

            var u2 = User.Create(Email.Create("u2@demo.com"), name, TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            db.Users.Add(u2);

            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        [Fact]
        public async Task Email_Normalization_Allows_MixedCase_Lookup()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var mixed = Email.Create("SaMe@e.CoM");
            var u = User.Create(mixed, UserName.Create("User Name"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            db.Users.Add(u);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var found = await db.Users.SingleOrDefaultAsync(x => x.Email == Email.Create("same@e.com"));

            found.Should().NotBeNull();
            found!.Id.Should().Be(u.Id);
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Update()
        {
            await _fx.ResetAsync();
            var (sp, db) = DbHelper.BuildDb(_cs);

            var u = User.Create(Email.Create("concurrency@demo.com"), UserName.Create("Concurrency User"), TestDataFactory. Bytes(32), TestDataFactory.Bytes(16));
            db.Users.Add(u);
            await db.SaveChangesAsync();

            // keep stale rowversion
            var stale = u.RowVersion!.ToArray();

            // first update succeeds
            u.ChangeRole(UserRole.Admin);
            await db.SaveChangesAsync();

            // second context with stale original rowversion
            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.Users.SingleAsync(x => x.Id == u.Id);

            var entry = db2.Entry(same);
            entry.Property(x => x.RowVersion).OriginalValue = stale;

            same.ChangeRole(UserRole.User);

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        }
    }
}
