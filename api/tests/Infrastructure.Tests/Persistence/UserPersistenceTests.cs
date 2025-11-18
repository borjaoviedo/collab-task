using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestHelpers.Common;
using TestHelpers.Common.Testing;
using TestHelpers.Persistence;

namespace Infrastructure.Tests.Persistence
{
    [IntegrationTest]
    [SqlServerContainerTest]
    [Collection("SqlServerContainer")]
    public sealed class UserPersistenceTests(MsSqlContainerFixture fx)
    {
        private readonly MsSqlContainerFixture _fx = fx;
        private readonly string _cs = fx.ConnectionString;

        private readonly static string _userName = "User";
        private readonly static string _userEmail = "user@email.com";

        private readonly User _user = User.Create(
                Email.Create(_userEmail),
                UserName.Create(_userName),
                TestDataFactory.CreateHash(),
                TestDataFactory.CreateSalt());

        [Fact]
        public async Task Add_And_GetByEmail_Works()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            db.Users.Add(_user);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var found = await db.Users.SingleOrDefaultAsync(user => user.Email == _userEmail);

            found.Should().NotBeNull();
            found!.Id.Should().Be(_user.Id);
        }

        [Fact]
        public async Task GetByName_Works()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            db.Users.Add(_user);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var found = await db.Users.SingleOrDefaultAsync(user => user.Name == _userName);

            found.Should().NotBeNull();
            found.Name.Value.Should().Be(_userName);
        }

        [Fact]
        public async Task Unique_Index_On_Email()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var sameEmail = Email.Create(_userEmail);

            var user1 = User.Create(
                sameEmail,
                UserName.Create("first user name"),
                TestDataFactory.CreateHash(),
                TestDataFactory.CreateSalt());
            db.Users.Add(user1);

            await db.SaveChangesAsync();

            var user2 = User.Create(
                sameEmail,
                UserName.Create("second user name"),
                TestDataFactory.CreateHash(),
                TestDataFactory.CreateSalt());
            db.Users.Add(user2);

            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        [Fact]
        public async Task Unique_Index_On_Name()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var sameName = UserName.Create(_userName);

            var user1 = User.Create(
                Email.Create("first@email.com"),
                sameName,
                TestDataFactory.CreateHash(),
                TestDataFactory.CreateSalt());
            db.Users.Add(user1);

            await db.SaveChangesAsync();

            var user2 = User.Create(
                Email.Create("second@email.com"),
                sameName,
                TestDataFactory.CreateHash(),
                TestDataFactory.CreateSalt());
            db.Users.Add(user2);

            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        [Fact]
        public async Task Email_Normalization_Allows_MixedCase_Lookup()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var mixedEmailStr = "SaMe@e.CoM";
            var mixedEmail = Email.Create(mixedEmailStr);

            var user = User.Create(
                mixedEmail,
                UserName.Create(_userName),
                TestDataFactory.CreateHash(),
                TestDataFactory.CreateSalt());
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var found = await db.Users.SingleOrDefaultAsync(user => user.Email == Email.Create(mixedEmailStr));

            found.Should().NotBeNull();
            found.Id.Should().Be(user.Id);
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Update()
        {
            await _fx.ResetAsync();
            var (sp, db) = DbHelper.BuildDb(_cs);

            db.Users.Add(_user);
            await db.SaveChangesAsync();

            // Keep stale rowversion
            var stale = _user.RowVersion!.ToArray();

            // First update succeeds
            _user.ChangeRole(UserRole.Admin);
            await db.SaveChangesAsync();

            // Second context with stale original rowversion
            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<CollabTaskDbContext>();
            var same = await db2.Users.SingleAsync(user => user.Id == _user.Id);

            var entry = db2.Entry(same);
            entry.Property(x => x.RowVersion).OriginalValue = stale;

            same.ChangeRole(UserRole.User);

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        }
    }
}
