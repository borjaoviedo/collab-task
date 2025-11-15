using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Common;
using TestHelpers.Persistence;

namespace Infrastructure.Tests.Repositories
{
    public sealed class TaskAssignmentRepositoryTests
    {
        [Fact]
        public async Task GetByTaskAndUserIdAsync_Returns_Assignment_Or_Null()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskAssignmentRepository(db);

            var (_, _, _, taskId, userId) = TestDataFactory.SeedColumnWithTask(db);
            TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.Owner);

            var found = await repo.GetByTaskAndUserIdAsync(taskId, userId);
            found.Should().NotBeNull();

            var notFound = await repo.GetByTaskAndUserIdAsync(taskId, userId: Guid.NewGuid());
            notFound.Should().BeNull();
        }

        [Fact]
        public async Task ListByTaskIdAsync_And_ListByUserAsync_Return_Collections()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskAssignmentRepository(db);

            var (_, _, _, taskId, userId) = TestDataFactory.SeedColumnWithTask(db);
            TestDataFactory.SeedTaskAssignment(db, taskId, userId, TaskRole.Owner);

            var byTask = await repo.ListByTaskIdAsync(taskId);
            byTask.Should().ContainSingle();

            var byUser = await repo.ListByUserIdAsync(userId);
            byUser.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Exists_AnyOwner_Work_As_Expected()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskAssignmentRepository(db);

            var (_, _, _, taskId, userId) = TestDataFactory.SeedColumnWithTask(db);

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

        // --------------- Add / Update / Remove ---------------

        [Fact]
        public async Task AddAsync_Persists_Assignment()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var uow = new UnitOfWork(db);
            var repo = new TaskAssignmentRepository(db);

            var (_, _, _, taskId, userId) = TestDataFactory.SeedColumnWithTask(db);

            var assignment = TaskAssignment.Create(taskId, userId, TaskRole.Owner);
            await repo.AddAsync(assignment);

            await uow.SaveAsync(MutationKind.Create);

            var fromDb = await db.TaskAssignments.AsNoTracking()
                .SingleAsync(a => a.TaskId == taskId && a.UserId == userId);
            fromDb.Role.Should().Be(TaskRole.Owner);
        }

        [Fact]
        public async Task UpdateAsync_Marks_Entity_Modified_And_Persists_Changes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskAssignmentRepository(db);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);
            var assignment = await repo.GetByTaskAndUserIdForUpdateAsync(taskId, userId);

            assignment!.Role.Should().Be(TaskRole.Owner); // owner by default

            // Modify through domain behavior
            assignment!.ChangeRole(TaskRole.CoOwner);

            await repo.UpdateAsync(assignment);
            await db.SaveChangesAsync();

            var reloaded = await db.TaskAssignments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == userId && a.TaskId == taskId);

            reloaded.Should().NotBeNull();
            reloaded!.Role.Should().Be(TaskRole.CoOwner); // coowner after role change
        }

        [Fact]
        public async Task RemoveAsync_Deletes_User_On_SaveChanges()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskAssignmentRepository(db);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);
            var assignment = await repo.GetByTaskAndUserIdForUpdateAsync(taskId, userId);

            await repo.RemoveAsync(assignment!);
            await db.SaveChangesAsync();

            var reloaded = await db.TaskAssignments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == userId && a.TaskId == taskId);

            reloaded.Should().BeNull();
        }
    }
}
