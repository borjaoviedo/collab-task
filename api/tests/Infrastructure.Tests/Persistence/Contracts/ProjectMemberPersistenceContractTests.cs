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
    public sealed class ProjectMemberPersistenceContractTests(MsSqlContainerFixture fx)
    {
        private readonly MsSqlContainerFixture _fx = fx;
        private readonly string _cs = fx.ConnectionString;

        [Fact]
        public async Task Unique_Index_ProjectId_UserId_Is_Enforced()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var owner = User.Create(Email.Create("o@demo.com"), UserName.Create("Owner"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            var u = User.Create(Email.Create("m@demo.com"), UserName.Create("Member"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            var p = Project.Create(owner.Id, ProjectName.Create("Alpha"));

            db.AddRange(owner, u, p);
            await db.SaveChangesAsync();

            var m1 = ProjectMember.Create(p.Id, u.Id, ProjectRole.Member);
            db.ProjectMembers.Add(m1);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // duplicate key for (ProjectId, UserId)
            var m2 = ProjectMember.Create(p.Id, u.Id, ProjectRole.Member);
            db.ProjectMembers.Add(m2);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

            // Detach failing entity to avoid retry on next SaveChanges
            db.Entry(m2).State = EntityState.Detached;

            // Different user is allowed
            var other = User.Create(Email.Create("x@demo.com"), UserName.Create("Other"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            db.Users.Add(other);
            await db.SaveChangesAsync();

            var m3 = ProjectMember.Create(p.Id, other.Id, ProjectRole.Member);
            db.ProjectMembers.Add(m3);
            await db.SaveChangesAsync();

            var count = await db.ProjectMembers.CountAsync(pm => pm.ProjectId == p.Id);
            count.Should().Be(3);
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Update()
        {
            await _fx.ResetAsync();
            var (sp, db) = DbHelper.BuildDb(_cs);

            var owner = User.Create(Email.Create("o@demo.com"), UserName.Create("Owner"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            var u = User.Create(Email.Create("m@demo.com"), UserName.Create("Member"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            var p = Project.Create(owner.Id, ProjectName.Create("Alpha"));
            p.AddMember(u.Id, ProjectRole.Member);

            db.AddRange(owner, u, p);
            await db.SaveChangesAsync();

            var current = await db.ProjectMembers.AsNoTracking()
                .SingleAsync(x => x.ProjectId == p.Id && x.UserId == u.Id);
            var stale = current.RowVersion!.ToArray();

            // first update
            var tracked = await db.ProjectMembers.SingleAsync(x => x.ProjectId == p.Id && x.UserId == u.Id);
            tracked.ChangeRole(ProjectRole.Admin);
            db.Entry(tracked).Property(x => x.Role).IsModified = true;
            await db.SaveChangesAsync();

            // second context with stale token
            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.ProjectMembers.SingleAsync(x => x.ProjectId == p.Id && x.UserId == u.Id);

            var entry = db2.Entry(same);
            entry.Property(x => x.RowVersion).OriginalValue = stale;
            same.ChangeRole(ProjectRole.Member);
            entry.Property(x => x.Role).IsModified = true;

            await FluentActions.Invoking(() => db2.SaveChangesAsync())
                .Should().ThrowAsync<DbUpdateConcurrencyException>();
        }

        [Fact]
        public async Task RemovedAt_Can_Be_Set_And_Cleared_Persists_Correctly()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var now = DateTimeOffset.UtcNow;
            var owner = User.Create(Email.Create("o@demo.com"), UserName.Create("Owner"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            var u = User.Create(Email.Create("m@demo.com"), UserName.Create("Member"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            var p = Project.Create(owner.Id, ProjectName.Create("Alpha"));
            p.AddMember(u.Id, ProjectRole.Member);

            db.AddRange(owner, u, p);
            await db.SaveChangesAsync();

            // set RemovedAt
            var pm = await db.ProjectMembers.SingleAsync(x => x.ProjectId == p.Id && x.UserId == u.Id);
            var removeAt = now.AddMinutes(10);
            pm.Remove(removeAt);
            db.Entry(pm).Property(x => x.RemovedAt).IsModified = true;
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var removed = await db.ProjectMembers.AsNoTracking()
                .SingleAsync(x => x.ProjectId == p.Id && x.UserId == u.Id);
            removed.RemovedAt.Should().Be(removeAt);

            // clear RemovedAt
            var tracked = await db.ProjectMembers.SingleAsync(x => x.ProjectId == p.Id && x.UserId == u.Id);
            tracked.Restore();
            db.Entry(tracked).Property(x => x.RemovedAt).IsModified = true;
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var restored = await db.ProjectMembers.AsNoTracking()
                .SingleAsync(x => x.ProjectId == p.Id && x.UserId == u.Id);
            restored.RemovedAt.Should().BeNull();
        }

        [Fact]
        public async Task RemovedAt_Cannot_Be_Before_JoinedAt_CheckConstraint_Enforced()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var owner = User.Create(Email.Create("o@d.com"), UserName.Create("Owner"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            var u = User.Create(Email.Create("m@d.com"), UserName.Create("Member"), TestDataFactory.Bytes(32), TestDataFactory.Bytes(16));
            var now = DateTimeOffset.UtcNow;
            var p = Project.Create(owner.Id, ProjectName.Create("Alpha"));
            db.AddRange(owner, u, p);
            await db.SaveChangesAsync();

            var pm = ProjectMember.Create(p.Id, u.Id, ProjectRole.Member);
            pm.RemovedAt = now.AddMinutes(-5);
            db.ProjectMembers.Add(pm);

            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }
}
