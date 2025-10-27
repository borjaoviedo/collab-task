using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
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
            await using var db = dbh.CreateContext();
            var uow = new UnitOfWork(db);
            var repo = new TaskAssignmentRepository(db);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            var assignment = TaskAssignment.Create(taskId, userId, TaskRole.Owner);
            await repo.AddAsync(assignment);

            await uow.SaveAsync(MutationKind.Create);

            var fromDb = await db.TaskAssignments.AsNoTracking()
                .SingleAsync(a => a.TaskId == taskId && a.UserId == userId);
            fromDb.Role.Should().Be(TaskRole.Owner);
        }

        [Fact]
        public async Task GetByTaskAndUserIdAsync_Returns_Assignment_Or_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskAssignmentRepository(db);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);
            TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.Owner);

            var found = await repo.GetByTaskAndUserIdAsync(taskId, userId);
            found.Should().NotBeNull();

            var notFound = await repo.GetByTaskAndUserIdAsync(taskId, Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task ListByTaskAsync_And_ListByUserAsync_Return_Collections()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
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
        public async Task Exists_AnyOwner_Work_As_Expected()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskAssignmentRepository(db);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            (await repo.ExistsAsync(taskId, userId)).Should().BeFalse();
            (await repo.AnyOwnerAsync(taskId)).Should().BeFalse();

            TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.Owner);

            (await repo.ExistsAsync(taskId, userId)).Should().BeTrue();
            (await repo.AnyOwnerAsync(taskId)).Should().BeTrue();
        }

        [Fact]
        public async Task ChangeRoleAsync_Updates_Or_Returns_NotFound_NoOp_Conflict()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var uow = new UnitOfWork(db);
            var repo = new TaskAssignmentRepository(db);

            var (_, userA) = TestDataFactory.SeedUserWithProject(db);
            var userB = TestDataFactory.SeedUser(db).Id;
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            // NotFound
            var (nf, _) = await repo.ChangeRoleAsync(taskId, userA, TaskRole.CoOwner, [1, 2]);
            nf.Should().Be(PrecheckStatus.NotFound);

            // Create A Owner
            var assignmentA = TaskAssignment.Create(taskId, userA, TaskRole.Owner);
            await repo.AddAsync(assignmentA);
            await uow.SaveAsync(MutationKind.Create);

            // NoOp
            var (no, _) = await repo.ChangeRoleAsync(taskId, userA, TaskRole.Owner, [1, 2]);
            no.Should().Be(PrecheckStatus.NoOp);

            // Create B CoOwner
            var assignmentB = TaskAssignment.Create(taskId, userB, TaskRole.CoOwner);
            await repo.AddAsync(assignmentB);
            await uow.SaveAsync(MutationKind.Create);

            // Conflict promoting B to Owner while A is Owner
            var (cf1, _) = await repo.ChangeRoleAsync(taskId, userB, TaskRole.Owner, [1, 2]);
            cf1.Should().Be(PrecheckStatus.Conflict);

            var (updStageWrongRv, _) = await repo.ChangeRoleAsync(taskId, userA, TaskRole.CoOwner, [1, 2]);
            updStageWrongRv.Should().Be(PrecheckStatus.Ready);
            var cf2 = await uow.SaveAsync(MutationKind.Update);
            cf2.Should().Be(DomainMutation.Conflict);

            db.ChangeTracker.Clear();
            var rvUserA = await db.TaskAssignments.AsNoTracking()
                                .Where(a => a.TaskId == taskId && a.UserId == userA)
                                .Select(a => a.RowVersion)
                                .SingleAsync();

            var (updAStage, _) = await repo.ChangeRoleAsync(taskId, userA, TaskRole.CoOwner, rvUserA!);
            updAStage.Should().Be(PrecheckStatus.Ready);
            var up = await uow.SaveAsync(MutationKind.Update);
            up.Should().Be(DomainMutation.Updated);

            db.ChangeTracker.Clear();
            var rvUserB = await db.TaskAssignments.AsNoTracking()
                                .Where(a => a.TaskId == taskId && a.UserId == userB)
                                .Select(a => a.RowVersion)
                                .SingleAsync();

            var (updBStage, _) = await repo.ChangeRoleAsync(taskId, userB, TaskRole.Owner, rvUserB!);
            updBStage.Should().Be(PrecheckStatus.Ready);
            var up2 = await uow.SaveAsync(MutationKind.Update);
            up2.Should().Be(DomainMutation.Updated);
        }

        [Fact]
        public async Task DeleteAsync_Deletes_NotFound_Or_Conflict()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var uow = new UnitOfWork(db);
            var repo = new TaskAssignmentRepository(db);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            // NotFound
            var nf = await repo.DeleteAsync(taskId, userId, [1, 2]);
            nf.Should().Be(PrecheckStatus.NotFound);

            var assignment = TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.Owner);

            await repo.DeleteAsync(taskId, userId, [1, 2]);
            var con = await uow.SaveAsync(MutationKind.Delete);
            con.Should().Be(DomainMutation.Conflict);

            await repo.DeleteAsync(taskId, userId, assignment.RowVersion!);
            var del = await uow.SaveAsync(MutationKind.Delete);
            del.Should().Be(DomainMutation.Deleted);

            (await repo.ExistsAsync(taskId, userId)).Should().BeFalse();
        }
    }
}
