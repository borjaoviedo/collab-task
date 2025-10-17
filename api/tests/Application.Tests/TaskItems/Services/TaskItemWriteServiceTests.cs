using Application.TaskActivities.Services;
using Application.TaskItems.Realtime;
using Application.TaskItems.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using TestHelpers;

namespace Application.Tests.TaskItems.Services
{
    public sealed class TaskItemWriteServiceTests
    {
        [Fact]
        public async Task CreateAsync_Returns_Created_And_Task_And_Publishes_Event()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();

            var svc = new TaskItemWriteService(taskRepo, actSvc, mediator.Object);

            var taskTitle = "Task Title";
            var taskDescription = "Description";
            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);

            var (res, task) = await svc.CreateAsync(pId, lId, cId, user.Id, taskTitle, taskDescription);

            res.Should().Be(DomainMutation.Created);
            task.Should().NotBeNull();

            mediator.Verify(m => m.Publish(
                It.Is<TaskItemCreated>(n => n.ProjectId == pId && n.Payload.TaskId == task!.Id),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EditAsync_Returns_Updated_When_A_Property_Changes_And_Publishes_Event()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskItemWriteService(taskRepo, actSvc, mediator.Object);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var newTitle = "New Title";
            var res = await svc.EditAsync(task.Id, user.Id, newTitle, newDescription: null, newDueDate: null, task.RowVersion);
            res.Should().Be(DomainMutation.Updated);

            var fromDb = await db.TaskItems.AsNoTracking().SingleAsync();
            fromDb.Title.Value.Should().Be(newTitle);

            mediator.Verify(m => m.Publish(
                It.Is<TaskItemUpdated>(n => n.ProjectId == pId && n.Payload.TaskId == task.Id && n.Payload.NewTitle == newTitle),
                It.IsAny<CancellationToken>()),
                Times.Once);

            var newDescription = "New Description";
            res = await svc.EditAsync(task.Id, user.Id, newTitle: null, newDescription, newDueDate: null, fromDb.RowVersion);
            res.Should().Be(DomainMutation.Updated);

            fromDb = await db.TaskItems.AsNoTracking().SingleAsync();
            fromDb.Description!.Value.Should().Be(newDescription);
        }

        [Fact]
        public async Task EditAsync_Returns_NoOp_When_No_Property_Changes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskItemWriteService(taskRepo, actSvc, mediator.Object);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);

            var sameTitle = "Title";
            var sameDescription = "Description";
            var sameDueDate = DateTimeOffset.UtcNow.AddDays(10);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId, sameTitle, sameDescription, sameDueDate);

            var res = await svc.EditAsync(task.Id, user.Id, sameTitle, sameDescription, sameDueDate, task.RowVersion);
            res.Should().Be(DomainMutation.NoOp);

            var fromDb = await db.TaskItems.AsNoTracking().SingleAsync();
            fromDb.Title.Value.Should().Be(sameTitle);
            fromDb.Description!.Value.Should().Be(sameDescription);
            fromDb.DueDate.Should().Be(sameDueDate);

            mediator.Verify(m => m.Publish(It.IsAny<TaskItemUpdated>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task EditAsync_Returns_Conflict_On_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskItemWriteService(taskRepo, actSvc, mediator.Object);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);

            var oldTitle = "Old";
            var newTitle = "New";
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId, oldTitle);

            var res = await svc.EditAsync(task.Id, user.Id, newTitle, newDescription: null, newDueDate: null, rowVersion: [1, 2]);
            res.Should().Be(DomainMutation.Conflict);

            var fromDb = await db.TaskItems.AsNoTracking().SingleAsync();
            fromDb.Title.Value.Should().Be(oldTitle);

            mediator.Verify(m => m.Publish(It.IsAny<TaskItemUpdated>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task MoveAsync_Returns_Updated_And_Publishes_Event_When_Moving_To_Different_Lane_And_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskItemWriteService(taskRepo, actSvc, mediator.Object);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var differentLane = TestDataFactory.SeedLane(db, pId, order: 1);
            var differentColumn = TestDataFactory.SeedColumn(db, pId, differentLane.Id);
            await db.SaveChangesAsync();

            var res = await svc.MoveAsync(task.Id, differentColumn.Id, differentLane.Id, user.Id, targetSortKey: 1, task.RowVersion);
            res.Should().Be(DomainMutation.Updated);

            mediator.Verify(m => m.Publish(
                It.Is<TaskItemMoved>(n => n.ProjectId == pId &&
                                          n.Payload.TaskId == task.Id &&
                                          n.Payload.ToLaneId == differentLane.Id &&
                                          n.Payload.ToColumnId == differentColumn.Id &&
                                          n.Payload.SortKey == 1m),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task MoveAsync_Returns_Updated_When_Moving_To_Same_Lane_And_Different_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskItemWriteService(taskRepo, actSvc, mediator.Object);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var differentColumn = TestDataFactory.SeedColumn(db, pId, lId, order: 1);
            await db.SaveChangesAsync();

            var res = await svc.MoveAsync(task.Id, differentColumn.Id, lId, user.Id, targetSortKey: 1, task.RowVersion);
            res.Should().Be(DomainMutation.Updated);
        }

        [Fact]
        public async Task MoveAsync_Returns_Updated_When_Moving_To_Same_Lane_And_Column_But_Different_SortKey()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskItemWriteService(taskRepo, actSvc, mediator.Object);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var res = await svc.MoveAsync(task.Id, cId, lId, user.Id, targetSortKey: 1, task.RowVersion);
            res.Should().Be(DomainMutation.Updated);
        }

        [Fact]
        public async Task MoveAsync_Returns_NoOp_When_Moving_To_Same_Lane_Column_And_SortKey()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskItemWriteService(taskRepo, actSvc, mediator.Object);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var res = await svc.MoveAsync(task.Id, cId, lId, user.Id, targetSortKey: 0, task.RowVersion);
            res.Should().Be(DomainMutation.NoOp);

            mediator.Verify(m => m.Publish(It.IsAny<TaskItemMoved>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task MoveAsync_Returns_NotFound_When_Moving_To_Non_Existing_Lane_Or_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskItemWriteService(taskRepo, actSvc, mediator.Object);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var res = await svc.MoveAsync(task.Id, Guid.NewGuid(), lId, user.Id, targetSortKey: 0, task.RowVersion);
            res.Should().Be(DomainMutation.NotFound);

            res = await svc.MoveAsync(task.Id, cId, Guid.NewGuid(), user.Id, targetSortKey: 0, task.RowVersion);
            res.Should().Be(DomainMutation.NotFound);

            mediator.Verify(m => m.Publish(It.IsAny<TaskItemMoved>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task MoveAsync_Returns_Conflict_When_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskItemWriteService(taskRepo, actSvc, mediator.Object);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var res = await svc.MoveAsync(task.Id, cId, lId, user.Id, targetSortKey: 1, rowVersion: [1, 2]);
            res.Should().Be(DomainMutation.Conflict);

            mediator.Verify(m => m.Publish(It.IsAny<TaskItemMoved>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task MoveAsync_Returns_Conflict_When_Moving_To_Other_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskItemWriteService(taskRepo, actSvc, mediator.Object);

            var (firstProjectId, firstProjectLaneId, firstProjectColumnId) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, firstProjectId, firstProjectLaneId, firstProjectColumnId);

            var (_, secondProjectLaneId, secondProjectColumnId) = TestDataFactory.SeedLaneWithColumn(db);
            await db.SaveChangesAsync();

            var res = await svc.MoveAsync(task.Id, secondProjectColumnId, secondProjectLaneId, user.Id, targetSortKey: 1, task.RowVersion);
            res.Should().Be(DomainMutation.Conflict);

            mediator.Verify(m => m.Publish(It.IsAny<TaskItemMoved>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Returns_Deleted_When_No_Problem()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskItemWriteService(taskRepo, actSvc, mediator.Object);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var res = await svc.DeleteAsync(task.Id, task.RowVersion);

            res.Should().Be(DomainMutation.Deleted);
            mediator.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DeleteAsync_Returns_Conflict_When_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskItemWriteService(taskRepo, actSvc, mediator.Object);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var res = await svc.DeleteAsync(task.Id, [9, 9, 9, 9]);

            res.Should().Be(DomainMutation.Conflict);
            mediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Returns_NotFound_When_Random_Id()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var actRepo = new TaskActivityRepository(db);
            var actSvc = new TaskActivityWriteService(actRepo);
            var mediator = new Mock<IMediator>();
            var svc = new TaskItemWriteService(taskRepo, actSvc, mediator.Object);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            TestDataFactory.SeedUser(db);
            TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var res = await svc.DeleteAsync(Guid.NewGuid(), [1, 2, 3, 4]);

            res.Should().Be(DomainMutation.NotFound);
            mediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
