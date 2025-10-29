using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers;

namespace Infrastructure.Tests.Repositories
{
    public sealed class TaskItemRepositoryTests
    {
        [Fact]
        public async Task AddAsync_Persists_TaskItem()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);

            var (projectId, laneId, columnId) = TestDataFactory.SeedLaneWithColumn(db);
            var taskTitle = "Task title";
            var taskDescription = "Description";

            var task = TaskItem.Create(
                columnId,
                laneId,
                projectId,
                TaskTitle.Create(taskTitle),
                TaskDescription.Create(taskDescription));
            await repo.AddAsync(task);
            await uow.SaveAsync(MutationKind.Create);

            var fromDb = await db.TaskItems.AsNoTracking().SingleAsync(t => t.Id == task.Id);
            fromDb.Title.Value.Should().Be(taskTitle);
            fromDb.Description.Value.Should().Be(taskDescription);
        }

        [Fact]
        public async Task ExistsWithTitleAsync_Returns_True_When_Duplicate_In_Same_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);

            var (projectId, laneId, columnId) = TestDataFactory.SeedLaneWithColumn(db);
            var taskTitle = TaskTitle.Create("Dup");
            TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId, TaskTitle.Create(taskTitle));

            var exists = await repo.ExistsWithTitleAsync(columnId, taskTitle);
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ListByColumnAsync_Returns_Sorted_By_SortKey()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);

            var (projectId, laneId, columnId) = TestDataFactory.SeedLaneWithColumn(db);
            var firstTaskTitle = TaskTitle.Create("Task Title A");
            var secondTaskTitle = TaskTitle.Create("Task Title B");
            var thirdTaskTitle = TaskTitle.Create("Task Title C");

            TestDataFactory.SeedTaskItem(
                db,
                projectId,
                laneId,
                columnId,
                firstTaskTitle,
                sortKey: 0m);
            TestDataFactory.SeedTaskItem(
                db,
                projectId,
                laneId,
                columnId,
                secondTaskTitle,
                sortKey: 1m);
            TestDataFactory.SeedTaskItem(
                db,
                projectId,
                laneId,
                columnId,
                thirdTaskTitle,
                sortKey: 2m);

            var list = await repo.ListByColumnAsync(columnId);
            list.Select(t => t.Title.Value).Should().Equal(firstTaskTitle, secondTaskTitle, thirdTaskTitle);
        }

        [Fact]
        public async Task EditAsync_Updates_Changed_Fields()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);

            var (projectId, laneId, columnId) = TestDataFactory.SeedLaneWithColumn(db);
            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var tracked = await db.TaskItems.SingleAsync(t => t.Id == task.Id);
            var newDueDate = DateTimeOffset.UtcNow.AddDays(1);
            newDueDate = DateTimeOffset.FromUnixTimeMilliseconds(newDueDate.ToUnixTimeMilliseconds());

            var newTitle = "new title";
            var newDescription = "new desc";

            var (status, change) = await repo.EditAsync(
                task.Id,
                TaskTitle.Create(newTitle),
                TaskDescription.Create(newDescription),
                newDueDate,
                tracked.RowVersion);
            status.Should().Be(PrecheckStatus.Ready);
            change.Should().NotBeNull();

            var saveRes = await uow.SaveAsync(MutationKind.Update);
            saveRes.Should().Be(DomainMutation.Updated);

            var fromDb = await db.TaskItems
                .AsNoTracking()
                .SingleAsync(t => t.Id == task.Id);
            fromDb.Title.Value.Should().Be(newTitle);
            fromDb.Description.Value.Should().Be(newDescription);
            fromDb.DueDate.Should().Be(newDueDate);
        }

        [Fact]
        public async Task EditAsync_NoOp_When_Nothing_Changes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);

            var (projectId, laneId, columnId) = TestDataFactory.SeedLaneWithColumn(db);
            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var tracked = await db.TaskItems.SingleAsync(t => t.Id == task.Id);
            var (status, change) = await repo.EditAsync(
                task.Id,
                newTitle: null,
                newDescription: null,
                newDueDate: null,
                tracked.RowVersion);
            status.Should().Be(PrecheckStatus.NoOp);
            change.Should().BeNull();
        }

        [Fact]
        public async Task EditAsync_Conflict_When_Duplicate_Title_In_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);

            var (projectId, laneId, columnId) = TestDataFactory.SeedLaneWithColumn(db);
            var taskATitle = TaskTitle.Create("Task A Title");
            TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId, taskATitle);
            var taskB = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var trackedB = await db.TaskItems.SingleAsync(t => t.Id == taskB.Id);
            var (status, change) = await repo.EditAsync(
                taskB.Id,
                taskATitle,
                newDescription: null,
                newDueDate: null,
                trackedB.RowVersion);

            status.Should().Be(PrecheckStatus.Conflict);
            change.Should().BeNull();
        }

        [Fact]
        public async Task MoveAsync_Updates_Column_Lane_And_SortKey()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);

            // Board with two lanes and two columns
            var (projectId, firstLaneId, firstColumnId) = TestDataFactory.SeedLaneWithColumn(db);
            var secondLane = TestDataFactory.SeedLane(db, projectId, order: 1);
            var secondColumn = TestDataFactory.SeedColumn(db, projectId, secondLane.Id, order: 1);

            var task = TestDataFactory.SeedTaskItem(db, projectId, firstLaneId, firstColumnId);

            var tracked = await db.TaskItems.SingleAsync(t => t.Id == task.Id);
            var (status, change) = await repo.MoveAsync(
                task.Id,
                targetColumnId: secondColumn.Id,
                targetLaneId: secondLane.Id,
                targetProjectId: projectId,
                targetSortKey: 5m,
                tracked.RowVersion);
            status.Should().Be(PrecheckStatus.Ready);
            change.Should().NotBeNull();

            var saveRes = await uow.SaveAsync(MutationKind.Update);
            saveRes.Should().Be(DomainMutation.Updated);

            var fromDb = await db.TaskItems
                .AsNoTracking()
                .SingleAsync(t => t.Id == task.Id);
            fromDb.ColumnId.Should().Be(secondColumn.Id);
            fromDb.LaneId.Should().Be(secondLane.Id);
            fromDb.SortKey.Should().Be(5m);
        }

        [Fact]
        public async Task MoveAsync_Conflict_When_Column_Not_In_Lane_Or_Project_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);

            var (firstProjectId, firstLaneId, firstColumnId) = TestDataFactory.SeedLaneWithColumn(db);
            var (_, secondProjectLaneId, secondProjectColumnId) = TestDataFactory.SeedLaneWithColumn(db);

            var task = TestDataFactory.SeedTaskItem(db, firstProjectId, firstLaneId, firstColumnId);

            var tracked = await db.TaskItems.SingleAsync(t => t.Id == task.Id);
            var (status, change) = await repo.MoveAsync(
                task.Id,
                targetColumnId: secondProjectColumnId,
                targetLaneId: secondProjectLaneId,
                targetProjectId: firstProjectId,
                targetSortKey: 1m,
                tracked.RowVersion);
            status.Should().Be(PrecheckStatus.Conflict);
            change.Should().BeNull();
        }

        [Fact]
        public async Task GetNextSortKeyAsync_Returns_Zero_When_Empty()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);

            var (_, _, columnId) = TestDataFactory.SeedLaneWithColumn(db);
            var nextSortKey = await repo.GetNextSortKeyAsync(columnId);
            nextSortKey.Should().Be(0m);
        }

        [Fact]
        public async Task GetNextSortKeyAsync_Returns_MaxPlusOne_When_Not_Empty()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);

            var (projectId, laneId, columnId) = TestDataFactory.SeedLaneWithColumn(db);
            TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId, sortKey: 0m);
            TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId, sortKey: 5m);

            var nextSortKey = await repo.GetNextSortKeyAsync(columnId);
            nextSortKey.Should().Be(6m);
        }

        [Fact]
        public async Task DeleteAsync_Removes_TaskItem()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);

            var (projectId, laneId, columnId) = TestDataFactory.SeedLaneWithColumn(db);
            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var tracked = await db.TaskItems.SingleAsync(t => t.Id == task.Id);

            var result = await repo.DeleteAsync(tracked.Id, tracked.RowVersion);
            result.Should().Be(PrecheckStatus.Ready);

            await uow.SaveAsync(MutationKind.Delete);

            var exists = await db.TaskItems.AnyAsync(t => t.Id == task.Id);
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_NotFound_When_Unknown_Id()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);

            var result = await repo.DeleteAsync(taskId: Guid.NewGuid(), rowVersion: []); 
            result.Should().Be(PrecheckStatus.NotFound);
        }
    }
}
