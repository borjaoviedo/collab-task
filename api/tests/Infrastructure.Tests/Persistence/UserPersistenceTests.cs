using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Infrastructure.Tests.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Persistence
{
    [Collection(nameof(DbCollection))]
    public class UserPersistenceTests
    {
        private readonly MsSqlContainerFixture _fx;
        public UserPersistenceTests(MsSqlContainerFixture fx) => _fx = fx;
        public static byte[] Bytes(int n, byte fill = 0x5A) => Enumerable.Repeat(fill, n).ToArray();

        [Fact]
        public async Task Create_And_GetById_Email_And_Name()
        {
            using var scope = _fx.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var u = User.Create(Email.Create("john@demo.com"), UserName.Create("John Doe"), Bytes(32), Bytes(16));

            db.Users.Add(u);
            await db.SaveChangesAsync();

            var byId = await db.Users.FindAsync(u.Id);
            var byEmail = await db.Users.SingleAsync(x => x.Email == Email.Create("john@demo.com"));
            var byName = await db.Users.SingleAsync(x => x.Name == UserName.Create("John Doe"));

            byId!.Id.Should().Be(u.Id);
            byEmail.Id.Should().Be(u.Id);
            byId.CreatedAt.Should().NotBe(default);
            byId.UpdatedAt.Should().NotBe(default);
            byName.Id.Should().Be(u.Id);
        }

        [Fact]
        public async Task Email_Uniqueness_Throws_On_Duplicate()
        {
            using var scope = _fx.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var u1 = User.Create(Email.Create("dup@demo.com"), UserName.Create("First user"), Bytes(32), Bytes(16));
            var u2 = User.Create(Email.Create("dup@demo.com"), UserName.Create("Second user"), Bytes(32), Bytes(16));

            db.Users.Add(u1);
            await db.SaveChangesAsync();

            db.Users.Add(u2);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        [Fact]
        public async Task Name_Uniqueness_Throws_On_Duplicate()
        {
            using var scope = _fx.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var u1 = User.Create(Email.Create("user1@demo.com"), UserName.Create("User"), Bytes(32), Bytes(16));
            var u2 = User.Create(Email.Create("user2@demo.com"), UserName.Create("User"), Bytes(32), Bytes(16));

            db.Users.Add(u1);
            await db.SaveChangesAsync();

            db.Users.Add(u2);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        [Fact]
        public async Task UpdatedAt_Changes_On_Real_Update_Only()
        {
            using var scope = _fx.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var u = User.Create(Email.Create("tick@demo.com"), UserName.Create("John"), Bytes(32), Bytes(16));
            db.Users.Add(u);
            await db.SaveChangesAsync();

            var initialUpdated = u.UpdatedAt;

            // No-op update (only touch UpdatedAt via EF should be ignored)
            db.Entry(u).Property(x => x.UpdatedAt).IsModified = true;
            await db.SaveChangesAsync();
            u.UpdatedAt.Should().Be(initialUpdated);

            // Real update
            u.Role = UserRole.Admin;
            await db.SaveChangesAsync();
            u.UpdatedAt.Should().BeAfter(initialUpdated);
        }

        [Fact]
        public async Task Concurrency_RowVersion_Conflicts_On_Parallel_Update()
        {
            // Seed and capture the real Id
            Guid userId;
            await using (var s = _fx.Services.CreateAsyncScope())
            {
                var d = s.ServiceProvider.GetRequiredService<AppDbContext>();
                var u = User.Create(Email.Create("race@demo.com"), UserName.Create("R Name"), Bytes(32), Bytes(16));
                d.Users.Add(u);
                await d.SaveChangesAsync();
                userId = u.Id;
            }

            await using var sA = _fx.Services.CreateAsyncScope();
            await using var sB = _fx.Services.CreateAsyncScope();
            var dbA = sA.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbB = sB.ServiceProvider.GetRequiredService<AppDbContext>();

            // Read stale copy (no tracking) to get old RowVersion
            var b = await dbB.Users.AsNoTracking().SingleAsync(x => x.Id == userId);
            var staleVersion = b.RowVersion;

            // Competing update in A
            var aTracked = await dbA.Users.SingleAsync(x => x.Id == userId);
            aTracked.Role = UserRole.Admin;
            await dbA.SaveChangesAsync();

            // Stub attach with correct key and stale RowVersion
            var stub = User.Create(Email.Create("x@x.com"), UserName.Create("XX"), Bytes(32), Bytes(16));
            stub.Id = userId;                         // ensure key matches existing row
            dbB.Attach(stub);                         // state = Unchanged
            dbB.Entry(stub).Property(x => x.RowVersion).OriginalValue = staleVersion;
            dbB.Entry(stub).Property(x => x.Role).CurrentValue = UserRole.User;
            dbB.Entry(stub).Property(x => x.Role).IsModified = true; // only update Role

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());
        }
    }
}
