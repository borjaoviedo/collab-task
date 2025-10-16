using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers;

namespace Infrastructure.Tests.Repositories
{
    public sealed class TaskAssignmentRepositoryTests
    {
        [Fact]
        public async Task AddAsync_Persists_Assignment()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new TaskAssignmentRepository(db);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            var assignment = TaskAssignment.Create(taskId, userId, TaskRole.Owner);
            await repo.AddAsync(assignment);
            await repo.SaveCreateChangesAsync();

            var fromDb = await db.TaskAssignments.AsNoTracking()
                .SingleAsync(a => a.TaskId == taskId && a.UserId == userId);
            fromDb.Role.Should().Be(TaskRole.Owner);
        }

        [Fact]
        public async Task GetAsync_Returns_Assignment_Or_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new TaskAssignmentRepository(db);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);
            TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.Owner);

            var found = await repo.GetAsync(taskId, userId);
            found.Should().NotBeNull();

            var notFound = await repo.GetAsync(taskId, Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task ListByTaskAsync_And_ListByUserAsync_Return_Collections()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new TaskAssignmentRepository(db);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);
            TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.Owner);

            var byTask = await repo.ListByTaskAsync(taskId);
            byTask.Should().ContainSingle();

            var byUser = await repo.ListByUserAsync(userId);
            byUser.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Exists_AnyOwner_CountByRole_Work_As_Expected()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new TaskAssignmentRepository(db);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            (await repo.ExistsAsync(taskId, userId)).Should().BeFalse();
            (await repo.AnyOwnerAsync(taskId)).Should().BeFalse();
            (await repo.CountByRoleAsync(taskId, TaskRole.Owner)).Should().Be(0);

            TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.Owner);

            (await repo.ExistsAsync(taskId, userId)).Should().BeTrue();
            (await repo.AnyOwnerAsync(taskId)).Should().BeTrue();
            (await repo.CountByRoleAsync(taskId, TaskRole.Owner)).Should().Be(1);
        }

        [Fact]
        public async Task AssignAsync_Creates_And_Enforces_SingleOwner()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new TaskAssignmentRepository(db);

            var (_, userA) = TestDataFactory.SeedUserWithProject(db);
            var userB = TestDataFactory.SeedUser(db).Id;
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            var (m1, _) = await repo.AssignAsync(taskId, userA, TaskRole.Owner);
            m1.Should().Be(DomainMutation.Created);
            await repo.SaveCreateChangesAsync();

            var (m2, _) = await repo.AssignAsync(taskId, userA, TaskRole.Owner);
            m2.Should().Be(DomainMutation.NoOp);

            var (m3, _) = await repo.AssignAsync(taskId, userB, TaskRole.Owner);
            m3.Should().Be(DomainMutation.Conflict);
        }

        [Fact]
        public async Task ChangeRoleAsync_Updates_Or_Returns_NotFound_NoOp_Conflict()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new TaskAssignmentRepository(db);

            var (_, userA) = TestDataFactory.SeedUserWithProject(db);
            var userB = TestDataFactory.SeedUser(db).Id;
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            // NotFound
            var (nf, _) = await repo.ChangeRoleAsync(taskId, userA, TaskRole.CoOwner, [1, 2]);
            nf.Should().Be(DomainMutation.NotFound);

            // Create A Owner
            var (cA, _) = await repo.AssignAsync(taskId, userA, TaskRole.Owner);
            cA.Should().Be(DomainMutation.Created);
            await repo.SaveCreateChangesAsync();

            // NoOp
            var (no, _) = await repo.ChangeRoleAsync(taskId, userA, TaskRole.Owner, [1, 2]);
            no.Should().Be(DomainMutation.NoOp);

            // Create B CoOwner
            var (cB, _) = await repo.AssignAsync(taskId, userB, TaskRole.CoOwner);
            cB.Should().Be(DomainMutation.Created);
            await repo.SaveCreateChangesAsync();

            // Conflict promoting B to Owner while A is Owner
            var (cf1, _) = await repo.ChangeRoleAsync(taskId, userB, TaskRole.Owner, [1, 2]);
            cf1.Should().Be(DomainMutation.Conflict);

            var (updStageWrongRv, _) = await repo.ChangeRoleAsync(taskId, userA, TaskRole.CoOwner, new byte[] { 1, 2 });
            updStageWrongRv.Should().Be(DomainMutation.Updated);
            var cf2 = await repo.SaveUpdateChangesAsync();
            cf2.Should().Be(DomainMutation.Conflict);

            db.ChangeTracker.Clear();
            var rvUserA = await db.TaskAssignments.AsNoTracking()
                                .Where(a => a.TaskId == taskId && a.UserId == userA)
                                .Select(a => a.RowVersion)
                                .SingleAsync();

            var (updAStage, _) = await repo.ChangeRoleAsync(taskId, userA, TaskRole.CoOwner, rvUserA!);
            updAStage.Should().Be(DomainMutation.Updated);
            var up = await repo.SaveUpdateChangesAsync();
            up.Should().Be(DomainMutation.Updated);

            db.ChangeTracker.Clear();
            var rvUserB = await db.TaskAssignments.AsNoTracking()
                                .Where(a => a.TaskId == taskId && a.UserId == userB)
                                .Select(a => a.RowVersion)
                                .SingleAsync();

            var (updBStage, _) = await repo.ChangeRoleAsync(taskId, userB, TaskRole.Owner, rvUserB!);
            updBStage.Should().Be(DomainMutation.Updated);
            var up2 = await repo.SaveUpdateChangesAsync();
            up2.Should().Be(DomainMutation.Updated);
        }

        [Fact]
        public async Task RemoveAsync_Deletes_NotFound_Or_Conflict()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new TaskAssignmentRepository(db);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            // NotFound
            var nf = await repo.RemoveAsync(taskId, userId, [1, 2]);
            nf.Should().Be(DomainMutation.NotFound);

            var assignment = TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.Owner);

            await repo.RemoveAsync(taskId, userId, [1, 2]);
            var con = await repo.SaveUpdateChangesAsync();
            con.Should().Be(DomainMutation.Conflict);

            await repo.RemoveAsync(taskId, userId, assignment.RowVersion!);
            var del = await repo.SaveRemoveChangesAsync();
            del.Should().Be(DomainMutation.Deleted);

            (await repo.ExistsAsync(taskId, userId)).Should().BeFalse();
        }
    }
}
