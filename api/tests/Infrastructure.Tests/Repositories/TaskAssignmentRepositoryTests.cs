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

            var notFound = await repo.GetByTaskAndUserIdAsync(taskId, userId: Guid.NewGuid());
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

            var assignmentExists = await repo.ExistsAsync(taskId, userId);
            var taskHasOwner = await repo.AnyOwnerAsync(taskId);

            assignmentExists.Should().BeFalse();
            taskHasOwner.Should().BeFalse();

            TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.Owner);

            assignmentExists = await repo.ExistsAsync(taskId, userId);
            taskHasOwner = await repo.AnyOwnerAsync(taskId);

            assignmentExists.Should().BeTrue();
            taskHasOwner.Should().BeTrue();
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
            var (notFound, _) = await repo.ChangeRoleAsync(
                taskId,
                userA,
                TaskRole.CoOwner,
                rowVersion: [1, 2]);
            notFound.Should().Be(PrecheckStatus.NotFound);

            // Create A Owner
            var assignmentA = TaskAssignment.Create(taskId, userA, TaskRole.Owner);
            await repo.AddAsync(assignmentA);
            await uow.SaveAsync(MutationKind.Create);

            // NoOp
            var (noOp, _) = await repo.ChangeRoleAsync(
                taskId,
                userA,
                TaskRole.Owner,
                rowVersion: [1, 2]);
            noOp.Should().Be(PrecheckStatus.NoOp);

            // Create B CoOwner
            var assignmentB = TaskAssignment.Create(taskId, userB, TaskRole.CoOwner);
            await repo.AddAsync(assignmentB);
            await uow.SaveAsync(MutationKind.Create);

            // Conflict promoting B to Owner while A is Owner
            var (conflict1, _) = await repo.ChangeRoleAsync(
                taskId,
                userB,
                TaskRole.Owner,
                rowVersion: [1, 2]);
            conflict1.Should().Be(PrecheckStatus.Conflict);

            var (updStageWrongRv, _) = await repo.ChangeRoleAsync(
                taskId,
                userA,
                TaskRole.CoOwner,
                rowVersion: [1, 2]);
            updStageWrongRv.Should().Be(PrecheckStatus.Ready);
            var conflict2 = await uow.SaveAsync(MutationKind.Update);
            conflict2.Should().Be(DomainMutation.Conflict);

            db.ChangeTracker.Clear();
            var rvUserA = await db.TaskAssignments
                                .AsNoTracking()
                                .Where(a => a.TaskId == taskId && a.UserId == userA)
                                .Select(a => a.RowVersion)
                                .SingleAsync();

            var (updAStage, _) = await repo.ChangeRoleAsync(taskId, userA, TaskRole.CoOwner, rvUserA);
            updAStage.Should().Be(PrecheckStatus.Ready);
            var update = await uow.SaveAsync(MutationKind.Update);
            update.Should().Be(DomainMutation.Updated);

            db.ChangeTracker.Clear();
            var rvUserB = await db.TaskAssignments
                                .AsNoTracking()
                                .Where(a => a.TaskId == taskId && a.UserId == userB)
                                .Select(a => a.RowVersion)
                                .SingleAsync();

            var (updBStage, _) = await repo.ChangeRoleAsync(taskId, userB, TaskRole.Owner, rvUserB);
            updBStage.Should().Be(PrecheckStatus.Ready);
            var update2 = await uow.SaveAsync(MutationKind.Update);
            update2.Should().Be(DomainMutation.Updated);
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
            var notFound = await repo.DeleteAsync(taskId, userId, rowVersion: [1, 2]);
            notFound.Should().Be(PrecheckStatus.NotFound);

            var assignment = TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.Owner);

            await repo.DeleteAsync(taskId, userId, rowVersion: [1, 2]);
            var conflict = await uow.SaveAsync(MutationKind.Delete);
            conflict.Should().Be(DomainMutation.Conflict);

            await repo.DeleteAsync(taskId, userId, assignment.RowVersion);
            var delete = await uow.SaveAsync(MutationKind.Delete);
            delete.Should().Be(DomainMutation.Deleted);

            (await repo.ExistsAsync(taskId, userId)).Should().BeFalse();
        }
    }
}
