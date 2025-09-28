using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Infrastructure.Tests.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Users
{
    [Collection(nameof(DbCollection))]
    public class UserPersistenceTests
    {
        private readonly MsSqlContainerFixture _fx;
        public UserPersistenceTests(MsSqlContainerFixture fx) => _fx = fx;

        [Fact]
        public async Task Create_And_GetById_And_Email()
        {
            using var scope = _fx.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var u = new User
            {
                Id = Guid.NewGuid(),
                Email = Email.Create("john@demo.com"),
                PasswordHash = new byte[32],
                PasswordSalt = new byte[16],
                Role = UserRole.User
            };

            db.Users.Add(u);
            await db.SaveChangesAsync();

            var byId = await db.Users.FindAsync(u.Id);
            var byEmail = await db.Users.SingleAsync(x => x.Email == Email.Create("john@demo.com"));

            byId!.Id.Should().Be(u.Id);
            byEmail.Id.Should().Be(u.Id);
            byId.CreatedAt.Should().NotBe(default);
            byId.UpdatedAt.Should().NotBe(default);
        }

        [Fact]
        public async Task Email_Uniqueness_Throws_On_Duplicate()
        {
            using var scope = _fx.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var u1 = new User { Id = Guid.NewGuid(), Email = Email.Create("dup@demo.com"), PasswordHash = new byte[32], PasswordSalt = new byte[16] };
            var u2 = new User { Id = Guid.NewGuid(), Email = Email.Create("dup@demo.com"), PasswordHash = new byte[32], PasswordSalt = new byte[16] };

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

            var u = new User { Id = Guid.NewGuid(), Email = Email.Create("tick@demo.com"), PasswordHash = new byte[32], PasswordSalt = new byte[16] };
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
            var id = Guid.NewGuid();

            // Seed
            await using (var s = _fx.Services.CreateAsyncScope())
            {
                var d = s.ServiceProvider.GetRequiredService<AppDbContext>();
                d.Users.Add(new User { Id = id, Email = Email.Create("race@demo.com"), PasswordHash = new byte[32], PasswordSalt = new byte[16] });
                await d.SaveChangesAsync();
            }

            await using var sA = _fx.Services.CreateAsyncScope();
            await using var sB = _fx.Services.CreateAsyncScope();
            var dbA = sA.ServiceProvider.GetRequiredService<AppDbContext>();
            var dbB = sB.ServiceProvider.GetRequiredService<AppDbContext>();

            var a = await dbA.Users.AsNoTracking().SingleAsync(x => x.Id == id);      
            var b = await dbB.Users.AsNoTracking().SingleAsync(x => x.Id == id);
            var staleVersion = b.RowVersion;

            var aTracked = await dbA.Users.SingleAsync(x => x.Id == id);
            aTracked.Role = UserRole.Admin;
            await dbA.SaveChangesAsync();

            var stub = new User { Id = id, Email = Email.Create("race@demo.com") };
            dbB.Attach(stub);
            dbB.Entry(stub).Property(x => x.RowVersion).OriginalValue = staleVersion;
            dbB.Entry(stub).Property(x => x.Role).CurrentValue = UserRole.User;
            dbB.Entry(stub).Property(x => x.Role).IsModified = true;

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());
        }
    }
}
