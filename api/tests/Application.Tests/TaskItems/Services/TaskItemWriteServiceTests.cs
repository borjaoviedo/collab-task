using Application.Common.Abstractions.Time;
using Application.TaskActivities.Services;
using Application.TaskItems.Realtime;
using Application.TaskItems.Services;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using TestHelpers.Common;
using TestHelpers.Common.Time;
using TestHelpers.Persistence;

namespace Application.Tests.TaskItems.Services
{
    public sealed class TaskItemWriteServiceTests
    {
        private readonly IDateTimeProvider _clock = TestTime.FixedClock();

        [Fact]
        public async Task CreateAsync_Returns_Created_And_Task_And_Publishes_Event()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var taskWriteSvc = new TaskItemWriteService(
                taskRepo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var taskTitle = TaskTitle.Create("Task Title");
            var taskDescription = TaskDescription.Create("Description");
            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);

            var (result, task) = await taskWriteSvc.CreateAsync(
                projectId,
                laneId,
                columnId,
                user.Id,
                taskTitle,
                taskDescription);

            result.Should().Be(DomainMutation.Created);
            task.Should().NotBeNull();

            mediator.Verify(m => m.Publish(
                It.Is<TaskItemCreated>(n => n.ProjectId == projectId && n.Payload.TaskId == task!.Id),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EditAsync_Returns_Updated_When_A_Property_Changes_And_Publishes_Event()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var taskWriteSvc = new TaskItemWriteService(
                taskRepo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var newTitle = TaskTitle.Create("New Title");
            var result = await taskWriteSvc.EditAsync(
                projectId,
                task.Id,
                user.Id,
                newTitle,
                newDescription: null,
                newDueDate: null,
                task.RowVersion);
            result.Should().Be(DomainMutation.Updated);

            var fromDb = await db.TaskItems.AsNoTracking().SingleAsync();
            fromDb.Title.Value.Should().Be(newTitle);

            mediator.Verify(m => m.Publish(
                It.Is<TaskItemUpdated>(n => n.ProjectId == projectId && n.Payload.TaskId == task.Id && n.Payload.NewTitle == newTitle),
                It.IsAny<CancellationToken>()),
                Times.Once);

            var newDescription = TaskDescription.Create("New Description");
            result = await taskWriteSvc.EditAsync(
                projectId,
                task.Id,
                user.Id,
                newTitle: null,
                newDescription,
                newDueDate: null,
                fromDb.RowVersion);
            result.Should().Be(DomainMutation.Updated);

            fromDb = await db.TaskItems.AsNoTracking().SingleAsync();
            fromDb.Description!.Value.Should().Be(newDescription);
        }

        [Fact]
        public async Task EditAsync_Returns_NoOp_When_No_Property_Changes()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var taskWriteSvc = new TaskItemWriteService(
                taskRepo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);

            var sameTitle = TaskTitle.Create("Title");
            var sameDescription = TaskDescription.Create("Description");
            var sameDueDate = DateTimeOffset.UtcNow.AddDays(10);
            sameDueDate = DateTimeOffset.FromUnixTimeMilliseconds(sameDueDate.ToUnixTimeMilliseconds());
            var task = TestDataFactory.SeedTaskItem(
                db,
                projectId,
                laneId,
                columnId,
                sameTitle,
                sameDescription,
                sameDueDate);

            var result = await taskWriteSvc.EditAsync(
                projectId,
                task.Id,
                user.Id,
                sameTitle,
                sameDescription,
                sameDueDate,
                task.RowVersion);
            result.Should().Be(DomainMutation.NoOp);

            var fromDb = await db.TaskItems.AsNoTracking().SingleAsync();
            fromDb.Title.Value.Should().Be(sameTitle);
            fromDb.Description!.Value.Should().Be(sameDescription);
            fromDb.DueDate.Should().Be(sameDueDate);

            mediator.Verify(m => m.Publish(
                It.IsAny<TaskItemUpdated>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task EditAsync_Returns_Conflict_On_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);

            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();
            var taskWriteSvc = new TaskItemWriteService(
                taskRepo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);

            var oldTitle = TaskTitle.Create("Old");
            var newTitle = TaskTitle.Create("New");
            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId, oldTitle);

            var result = await taskWriteSvc.EditAsync(
                projectId,
                task.Id,
                user.Id,
                newTitle,
                newDescription: null,
                newDueDate: null,
                rowVersion: [1, 2]);
            result.Should().Be(DomainMutation.Conflict);

            var fromDb = await db.TaskItems.AsNoTracking().SingleAsync();
            fromDb.Title.Value.Should().Be(oldTitle);

            mediator.Verify(m => m.Publish(
                It.IsAny<TaskItemUpdated>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task MoveAsync_Returns_Updated_And_Publishes_Event_When_Moving_To_Different_Lane_And_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var taskWriteSvc = new TaskItemWriteService(
                taskRepo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var differentLane = TestDataFactory.SeedLane(db, projectId, order: 1);
            var differentColumn = TestDataFactory.SeedColumn(db, projectId, differentLane.Id);
            await db.SaveChangesAsync();

            var result = await taskWriteSvc.MoveAsync(
                projectId,
                task.Id,
                differentColumn.Id,
                differentLane.Id,
                user.Id,
                targetSortKey: 1,
                task.RowVersion);
            result.Should().Be(DomainMutation.Updated);

            mediator.Verify(m => m.Publish(
                It.Is<TaskItemMoved>(n => n.ProjectId == projectId &&
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
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var taskWriteSvc = new TaskItemWriteService(
                taskRepo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var differentColumn = TestDataFactory.SeedColumn(db, projectId, laneId, order: 1);
            await db.SaveChangesAsync();

            var result = await taskWriteSvc.MoveAsync(
                projectId,
                task.Id,
                differentColumn.Id,
                laneId,
                user.Id,
                targetSortKey: 1,
                task.RowVersion);
            result.Should().Be(DomainMutation.Updated);
        }

        [Fact]
        public async Task MoveAsync_Returns_Updated_When_Moving_To_Same_Lane_And_Column_But_Different_SortKey()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var taskWriteSvc = new TaskItemWriteService(
                taskRepo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var result = await taskWriteSvc.MoveAsync(
                projectId,
                task.Id,
                columnId,
                laneId,
                user.Id,
                targetSortKey: 1,
                task.RowVersion);
            result.Should().Be(DomainMutation.Updated);
        }

        [Fact]
        public async Task MoveAsync_Returns_NoOp_When_Moving_To_Same_Lane_Column_And_SortKey()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var taskWriteSvc = new TaskItemWriteService(
                taskRepo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var result = await taskWriteSvc.MoveAsync(
                projectId,
                task.Id,
                columnId,
                laneId,
                user.Id,
                targetSortKey: 0,
                task.RowVersion);
            result.Should().Be(DomainMutation.NoOp);

            mediator.Verify(m => m.Publish(
                It.IsAny<TaskItemMoved>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task MoveAsync_Returns_NotFound_When_Moving_To_Non_Existing_Lane_Or_Column()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var taskWriteSvc = new TaskItemWriteService(
                taskRepo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var result = await taskWriteSvc.MoveAsync(
                projectId,
                task.Id,
                targetColumnId: Guid.NewGuid(),
                laneId,
                user.Id,
                targetSortKey: 0,
                task.RowVersion);
            result.Should().Be(DomainMutation.NotFound);

            result = await taskWriteSvc.MoveAsync(
                projectId,
                task.Id,
                columnId,
                targetLaneId: Guid.NewGuid(),
                user.Id,
                targetSortKey: 0,
                task.RowVersion);
            result.Should().Be(DomainMutation.NotFound);

            mediator.Verify(m => m.Publish(
                It.IsAny<TaskItemMoved>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task MoveAsync_Returns_Conflict_When_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var taskWriteSvc = new TaskItemWriteService(
                taskRepo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var result = await taskWriteSvc.MoveAsync(
                projectId,
                task.Id,
                columnId,
                laneId,
                user.Id,
                targetSortKey: 1,
                rowVersion: [1, 2]);
            result.Should().Be(DomainMutation.Conflict);

            mediator.Verify(m => m.Publish(
                It.IsAny<TaskItemMoved>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task MoveAsync_Returns_Conflict_When_Moving_To_Other_Project()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var taskWriteSvc = new TaskItemWriteService(taskRepo, uow, activityWriteSvc, mediator.Object);

            var (firstProjectId, firstProjectLaneId, firstProjectColumnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, firstProjectId, firstProjectLaneId, firstProjectColumnId);

            var (projectId, secondProjectLaneId, secondProjectColumnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            await db.SaveChangesAsync();

            var result = await taskWriteSvc.MoveAsync(
                projectId,
                task.Id,
                secondProjectColumnId,
                secondProjectLaneId,
                user.Id,
                targetSortKey: 1,
                task.RowVersion);
            result.Should().Be(DomainMutation.Conflict);

            mediator.Verify(m => m.Publish(
                It.IsAny<TaskItemMoved>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Returns_Deleted_And_Publishes_TaskDeleted()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>(MockBehavior.Strict);

            var taskWriteSvc = new TaskItemWriteService(
                taskRepo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            mediator
                .Setup(m => m.Publish(
                    It.Is<TaskItemDeleted>(n =>
                                           n.ProjectId == projectId &&
                                           n.Payload.TaskId == task.Id),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var result = await taskWriteSvc.DeleteAsync(projectId, task.Id, task.RowVersion);

            result.Should().Be(DomainMutation.Deleted);

            mediator.Verify(m => m.Publish(
                It.Is<TaskItemDeleted>(n =>
                                       n.ProjectId == projectId &&
                                       n.Payload.TaskId == task.Id),
                It.IsAny<CancellationToken>()), Times.Once);

            mediator.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DeleteAsync_Returns_Conflict_When_RowVersion_Mismatch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var taskWriteSvc = new TaskItemWriteService(
                taskRepo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            TestDataFactory.SeedUser(db);
            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var result = await taskWriteSvc.DeleteAsync(projectId, task.Id, [9, 9, 9, 9]);

            result.Should().Be(DomainMutation.Conflict);
            mediator.Verify(m => m.Publish(
                It.IsAny<INotification>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Returns_NotFound_When_Random_Id()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var taskRepo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var mediator = new Mock<IMediator>();

            var taskWriteSvc = new TaskItemWriteService(
                taskRepo,
                uow,
                activityWriteSvc,
                mediator.Object);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            TestDataFactory.SeedUser(db);
            TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var result = await taskWriteSvc.DeleteAsync(
                projectId,
                taskId: Guid.NewGuid(),
                rowVersion: [1, 2, 3, 4]);

            result.Should().Be(DomainMutation.NotFound);
            mediator.Verify(m => m.Publish(
                It.IsAny<INotification>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
