using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestHelpers.Common;
using TestHelpers.Persistence;

namespace Infrastructure.Tests.Persistence
{
    [Collection("SqlServerContainer")]
    public sealed class ProjectMemberPersistenceTests(MsSqlContainerFixture fx)
    {
        private readonly MsSqlContainerFixture _fx = fx;
        private readonly string _cs = fx.ConnectionString;

        private readonly static byte[] _validHash = TestDataFactory.Bytes(32);
        private readonly static byte[] _validSalt = TestDataFactory.Bytes(16);

        private readonly User _owner = User.Create(
                Email.Create("o@demo.com"),
                UserName.Create("Owner"),
                _validHash,
                _validSalt);

        private readonly User _user = User.Create(
                Email.Create("m@demo.com"),
                UserName.Create("Member"),
                _validHash,
                _validSalt);

        [Fact]
        public async Task Unique_Index_ProjectId_UserId_Is_Enforced()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var project = Project.Create(_owner.Id, ProjectName.Create("Alpha"));

            db.AddRange(_owner, _user, project);
            await db.SaveChangesAsync();

            var member1 = ProjectMember.Create(project.Id, _user.Id, ProjectRole.Member);
            db.ProjectMembers.Add(member1);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            // Duplicate key for (ProjectId, UserId)
            var member2 = ProjectMember.Create(project.Id, _user.Id, ProjectRole.Member);
            db.ProjectMembers.Add(member2);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

            // Detach failing entity to avoid retry on next SaveChanges
            db.Entry(member2).State = EntityState.Detached;

            // Different user is allowed
            var other = User.Create(
                Email.Create("x@demo.com"),
                UserName.Create("Other"),
                _validHash,
                _validSalt);
            db.Users.Add(other);
            await db.SaveChangesAsync();

            var member3 = ProjectMember.Create(project.Id, other.Id, ProjectRole.Member);
            db.ProjectMembers.Add(member3);
            await db.SaveChangesAsync();

            var count = await db.ProjectMembers.CountAsync(projectMember => projectMember.ProjectId == project.Id);
            count.Should().Be(3);
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Update()
        {
            await _fx.ResetAsync();
            var (sp, db) = DbHelper.BuildDb(_cs);

            var project = Project.Create(_owner.Id, ProjectName.Create("Alpha"));
            project.AddMember(_user.Id, ProjectRole.Member);

            db.AddRange(_owner, _user, project);
            await db.SaveChangesAsync();

            var current = await db.ProjectMembers
                .AsNoTracking()
                .SingleAsync(m => m.ProjectId == project.Id && m.UserId == _user.Id);
            var stale = current.RowVersion!.ToArray();

            // First update
            var tracked = await db.ProjectMembers.SingleAsync(m => m.ProjectId == project.Id && m.UserId == _user.Id);
            tracked.ChangeRole(ProjectRole.Admin);
            db.Entry(tracked).Property(m => m.Role).IsModified = true;
            await db.SaveChangesAsync();

            // Second context with stale token
            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.ProjectMembers.SingleAsync(m => m.ProjectId == project.Id && m.UserId == _user.Id);

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
            var project = Project.Create(_owner.Id, ProjectName.Create("Alpha"));
            project.AddMember(_user.Id, ProjectRole.Member);

            db.AddRange(_owner, _user, project);
            await db.SaveChangesAsync();

            // Remove
            var projectMember = await db.ProjectMembers.SingleAsync(x => x.ProjectId == project.Id && x.UserId == _user.Id);
            var removeAt = now.AddMinutes(10);
            projectMember.Remove(removeAt);
            db.Entry(projectMember).Property(x => x.RemovedAt).IsModified = true;
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var removed = await db.ProjectMembers.AsNoTracking()
                .SingleAsync(x => x.ProjectId == project.Id && x.UserId == _user.Id);
            removed.RemovedAt.Should().Be(removeAt);

            // Restore
            var tracked = await db.ProjectMembers.SingleAsync(x => x.ProjectId == project.Id && x.UserId == _user.Id);
            tracked.Restore();
            db.Entry(tracked).Property(x => x.RemovedAt).IsModified = true;
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();
            var restored = await db.ProjectMembers.AsNoTracking()
                .SingleAsync(x => x.ProjectId == project.Id && x.UserId == _user.Id);
            restored.RemovedAt.Should().BeNull();
        }

        [Fact]
        public async Task RemovedAt_Cannot_Be_Before_JoinedAt_CheckConstraint_Enforced()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var now = DateTimeOffset.UtcNow;
            var project = Project.Create(_owner.Id, ProjectName.Create("Alpha"));

            db.AddRange(_owner, _user, project);
            await db.SaveChangesAsync();

            var projectMember = ProjectMember.Create(project.Id, _user.Id, ProjectRole.Member);
            var removedAt = now.AddMinutes(-5);

            projectMember.Remove(removedAt);
            db.ProjectMembers.Add(projectMember);

            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }
}
