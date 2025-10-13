using Application.TaskItems.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using TestHelpers;

namespace Application.Tests.TaskItems.Services
{
    public sealed class TaskItemWriteServiceTests
    {
        [Fact]
        public async Task CreateAsync_Returns_Created_And_Id()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemWriteService(repo);

            var taskTitle = "Task Title";
            var taskDescription = "Description";
            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var (res, id) = await svc.CreateAsync(pId, lId, cId, taskTitle, taskDescription);

            res.Should().Be(DomainMutation.Created);
            id.Should().NotBeNull();
        }

        [Fact]
        public async Task EditAsync_Returns_Updated_When_A_Property_Changes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemWriteService(repo);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var newTitle = "New Title";
            var res = await svc.EditAsync(task.Id, newTitle, newDescription: null, newDueDate: null, task.RowVersion);
            res.Should().Be(DomainMutation.Updated);

            var fromDb = await db.TaskItems.AsNoTracking().SingleAsync();
            fromDb.Title.Value.Should().Be(newTitle);

            var newDescription = "New Description";
            res = await svc.EditAsync(task.Id, newTitle: null, newDescription, newDueDate: null, fromDb.RowVersion);
            res.Should().Be(DomainMutation.Updated);

            fromDb = await db.TaskItems.AsNoTracking().SingleAsync();
            fromDb.Description.Value.Should().Be(newDescription);

            var newDueDate = DateTimeOffset.UtcNow.AddDays(10);
            res = await svc.EditAsync(task.Id, newTitle: null, newDescription: null, newDueDate, fromDb.RowVersion);
            res.Should().Be(DomainMutation.Updated);

            fromDb = await db.TaskItems.AsNoTracking().SingleAsync();
            fromDb.DueDate.Should().Be(newDueDate);
        }

        [Fact]
        public async Task EditAsync_Returns_NoOp_When_No_Property_Changes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemWriteService(repo);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);

            var sameTitle = "Title";
            var sameDescription = "Description";
            var sameDueDate = DateTimeOffset.UtcNow.AddDays(10);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId, sameTitle, sameDescription, sameDueDate);

            var res = await svc.EditAsync(task.Id, sameTitle, sameDescription, sameDueDate, task.RowVersion);
            res.Should().Be(DomainMutation.NoOp);

            var fromDb = await db.TaskItems.AsNoTracking().SingleAsync();
            fromDb.Title.Value.Should().Be(sameTitle);
            fromDb.Description.Value.Should().Be(sameDescription);
            fromDb.DueDate.Should().Be(sameDueDate);
        }

        [Fact]
        public async Task EditAsync_Returns_Conflict_On_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemWriteService(repo);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);

            var oldTile = "Old";
            var newTile = "New";
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId, oldTile);

            var res = await svc.EditAsync(task.Id, newTile, newDescription: null, newDueDate: null, [1, 2]);
            res.Should().Be(DomainMutation.Conflict);

            var fromDb = await db.TaskItems.AsNoTracking().SingleAsync();
            fromDb.Title.Value.Should().Be(oldTile);
        }

        [Fact]
        public async Task MoveAsync_Returns_Updated_When_Moving_To_Different_Lane_And_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemWriteService(repo);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var differentLane = TestDataFactory.SeedLane(db, pId, order: 1);
            var differentColumn = TestDataFactory.SeedColumn(db, pId, differentLane.Id);

            var res = await svc.MoveAsync(task.Id, differentColumn.Id, differentLane.Id, targetSortKey: 1, task.RowVersion);
            res.Should().Be(DomainMutation.Updated);
        }

        [Fact]
        public async Task MoveAsync_Returns_Updated_When_Moving_To_Same_Lane_And_Different_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemWriteService(repo);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var differentColumn = TestDataFactory.SeedColumn(db, pId, lId, order: 1);

            var res = await svc.MoveAsync(task.Id, differentColumn.Id, lId, targetSortKey: 1, task.RowVersion);
            res.Should().Be(DomainMutation.Updated);
        }

        [Fact]
        public async Task MoveAsync_Returns_Updated_When_Moving_To_Same_Lane_And_Column_But_Different_SortKey()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemWriteService(repo);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var res = await svc.MoveAsync(task.Id, cId, lId, targetSortKey: 1, task.RowVersion);
            res.Should().Be(DomainMutation.Updated);
        }

        [Fact]
        public async Task MoveAsync_Returns_NoOp_When_Moving_To_Same_Lane_Column_And_SortKey()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemWriteService(repo);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var res = await svc.MoveAsync(task.Id, cId, lId, targetSortKey: 0, task.RowVersion);
            res.Should().Be(DomainMutation.NoOp);
        }

        [Fact]
        public async Task MoveAsync_Returns_NotFound_When_Moving_To_Non_Existing_Lane_Or_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemWriteService(repo);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var res = await svc.MoveAsync(task.Id, Guid.NewGuid(), lId, targetSortKey: 0, task.RowVersion);
            res.Should().Be(DomainMutation.NotFound);

            res = await svc.MoveAsync(task.Id, cId, Guid.NewGuid(), targetSortKey: 0, task.RowVersion);
            res.Should().Be(DomainMutation.NotFound);
        }

        [Fact]
        public async Task MoveAsync_Returns_Conflict_When_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemWriteService(repo);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var res = await svc.MoveAsync(task.Id, cId, lId, targetSortKey: 1, [1, 2]);
            res.Should().Be(DomainMutation.Conflict);
        }

        [Fact]
        public async Task MoveAsync_Returns_Conflict_When_Moving_To_Other_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemWriteService(repo);

            var (firstProjectId, firstProjectLaneId, firstProjectColumnId) = TestDataFactory.SeedLaneWithColumn(db);
            var task = TestDataFactory.SeedTaskItem(db, firstProjectId, firstProjectLaneId, firstProjectColumnId);

            var (_, secondProjectLaneId, secondProjectColumnId) = TestDataFactory.SeedLaneWithColumn(db);

            var res = await svc.MoveAsync(task.Id, secondProjectColumnId, secondProjectLaneId, targetSortKey: 1, task.RowVersion);
            res.Should().Be(DomainMutation.Conflict);
        }


        [Fact]
        public async Task DeleteAsync_Returns_Deleted_When_No_Problem()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemWriteService(repo);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var res = await svc.DeleteAsync(task.Id, task.RowVersion);

            res.Should().Be(DomainMutation.Deleted);
        }

        [Fact]
        public async Task DeleteAsync_Returns_Conflict_When_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemWriteService(repo);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var res = await svc.DeleteAsync(task.Id, [9, 9, 9, 9]);

            res.Should().Be(DomainMutation.Conflict);
        }

        [Fact]
        public async Task DeleteAsync_Returns_NotFound_When_Random_Id()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var svc = new TaskItemWriteService(repo);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var res = await svc.DeleteAsync(Guid.NewGuid(), task.RowVersion);

            res.Should().Be(DomainMutation.NotFound);
        }
    }
}
