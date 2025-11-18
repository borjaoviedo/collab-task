using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers.Common;
using TestHelpers.Common.Testing;
using TestHelpers.Persistence;

namespace Infrastructure.Tests.Repositories
{
    [IntegrationTest]
    public sealed class TaskItemRepositoryTests
    {
        [Fact]
        public async Task ExistsWithTitleAsync_Returns_True_When_Duplicate_In_Same_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var taskTitle = TaskTitle.Create("Dup");
            TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId, TaskTitle.Create(taskTitle));

            var exists = await repo.ExistsWithTitleAsync(columnId, taskTitle);
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task ListByColumnIdAsync_Returns_Sorted_By_SortKey()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
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

            var list = await repo.ListByColumnIdAsync(columnId);
            list.Select(t => t.Title.Value).Should().Equal(firstTaskTitle, secondTaskTitle, thirdTaskTitle);
        }


        [Fact]
        public async Task GetNextSortKeyAsync_Returns_Zero_When_Empty()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);

            var (_, _, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var nextSortKey = await repo.GetNextSortKeyAsync(columnId);
            nextSortKey.Should().Be(0m);
        }

        [Fact]
        public async Task GetNextSortKeyAsync_Returns_MaxPlusOne_When_Not_Empty()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);

            var     (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId, sortKey: 0m);
            TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId, sortKey: 5m);

            var nextSortKey = await repo.GetNextSortKeyAsync(columnId);
            nextSortKey.Should().Be(6m);
        }


        // --------------- Add / Update / Remove ---------------


        [Fact]
        public async Task AddAsync_Persists_TaskItem()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
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
        public async Task UpdateAsync_Marks_Entity_Modified_And_Persists_Changes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);

            var (_, _, _, taskId, _) = TestDataFactory.SeedColumnWithTask(db);
            var task = await repo.GetByIdForUpdateAsync(taskId);

            // Modify through domain behavior
            task!.Edit(title: TaskTitle.Create("Updated Title"), description: null, dueDate: null);

            await repo.UpdateAsync(task);
            await db.SaveChangesAsync();

            var reloaded = await db.TaskItems
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == taskId);

            reloaded.Should().NotBeNull();
            reloaded!.Title.Value.Should().Be("Updated Title");
        }

        [Fact]
        public async Task RemoveAsync_Deletes_User_On_SaveChanges()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);

            var (_, _, _, taskId, _) = TestDataFactory.SeedColumnWithTask(db);
            var task = await repo.GetByIdForUpdateAsync(taskId);

            await repo.RemoveAsync(task!);
            await db.SaveChangesAsync();

            var reloaded = await db.TaskItems
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == taskId);

            reloaded.Should().BeNull();
        }
    }
}
