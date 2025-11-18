using Application.Abstractions.Time;
using Application.Common.Exceptions;
using Application.TaskActivities.Services;
using Application.TaskItems.DTOs;
using Application.TaskItems.Realtime;
using Application.TaskItems.Services;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using TestHelpers.Common;
using TestHelpers.Common.Fakes;
using TestHelpers.Common.Testing;
using TestHelpers.Common.Time;
using TestHelpers.Persistence;

namespace Application.Tests.TaskItems.Services
{
    [IntegrationTest]
    public sealed class TaskItemWriteServiceTests
    {
        private static readonly IDateTimeProvider _clock = TestTime.FixedClock();

        [Fact]
        public async Task CreateAsync_Returns_Task_And_Publishes_Event()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, mediator, currentUser) = await CreateSutAsync(dbh);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            currentUser.UserId = user.Id;

            var createDto = new TaskItemCreateDto
            {
                Title = "Task Title",
                Description = "Description",
                DueDate = null,
                SortKey = 0m
            };

            var created = await writeSvc.CreateAsync(
                projectId,
                laneId,
                columnId,
                createDto);

            created.Should().NotBeNull();
            created.Title.Should().Be(createDto.Title);
            created.Description.Should().Be(createDto.Description);
            created.ProjectId.Should().Be(projectId);

            mediator.Verify(m => m.Publish(
                    It.Is<TaskItemCreated>(n =>
                        n.ProjectId == projectId &&
                        n.Payload.TaskId == created.Id),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EditAsync_Returns_Updated_When_A_Property_Changes_And_Publishes_Event()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, mediator, currentUser) = await CreateSutAsync(dbh);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            currentUser.UserId = user.Id;

            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var newTitle = "New Title";
            var newDescription = "New Description";

            var editDto = new TaskItemEditDto
            {
                NewTitle = newTitle,
                NewDescription = newDescription,
                NewDueDate = null
            };

            var updated = await writeSvc.EditAsync(
                projectId,
                task.Id,
                editDto);

            updated.Title.Should().Be(newTitle);
            updated.Description.Should().Be(newDescription);

            var fromDb = await db.TaskItems.AsNoTracking().SingleAsync();
            fromDb.Title.Value.Should().Be(newTitle);
            fromDb.Description!.Value.Should().Be(newDescription);

            mediator.Verify(m => m.Publish(
                    It.Is<TaskItemUpdated>(n =>
                        n.ProjectId == projectId &&
                        n.Payload.TaskId == task.Id &&
                        n.Payload.NewTitle == newTitle &&
                        n.Payload.NewDescription == newDescription),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EditAsync_Allows_ReSaving_Same_Values()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, mediator, currentUser) = await CreateSutAsync(dbh);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            currentUser.UserId = user.Id;

            var sameTitle = "Title";
            var sameDescription = "Description";
            var sameDueDate = DateTimeOffset.UtcNow.AddDays(10);
            sameDueDate = DateTimeOffset.FromUnixTimeMilliseconds(sameDueDate.ToUnixTimeMilliseconds());

            var task = TestDataFactory.SeedTaskItem(
                db,
                projectId,
                laneId,
                columnId,
                TaskTitle.Create(sameTitle),
                TaskDescription.Create(sameDescription),
                sameDueDate);

            var dto = new TaskItemEditDto
            {
                NewTitle = sameTitle,
                NewDescription = sameDescription,
                NewDueDate = sameDueDate
            };

            var updated = await writeSvc.EditAsync(
                projectId,
                task.Id,
                dto);

            updated.Title.Should().Be(sameTitle);
            updated.Description.Should().Be(sameDescription);
            updated.DueDate.Should().Be(sameDueDate);

            var fromDb = await db.TaskItems.AsNoTracking().SingleAsync();
            fromDb.Title.Value.Should().Be(sameTitle);
            fromDb.Description!.Value.Should().Be(sameDescription);
            fromDb.DueDate.Should().Be(sameDueDate);

            mediator.Verify(m => m.Publish(
                    It.Is<TaskItemUpdated>(n =>
                        n.ProjectId == projectId &&
                        n.Payload.TaskId == task.Id),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EditAsync_Throws_NotFound_When_Task_Does_Not_Exist()
        {
            using var dbh = new SqliteTestDb();
            var (_, writeSvc, mediator, currentUser) = await CreateSutAsync(dbh);

            var user = TestDataFactory.SeedUser(dbh.CreateContext());
            currentUser.UserId = user.Id;

            var randomProjectId = Guid.NewGuid();
            var randomTaskId = Guid.NewGuid();

            var dto = new TaskItemEditDto
            {
                NewTitle = "Title",
                NewDescription = "Description",
                NewDueDate = null
            };

            Func<Task> act = () => writeSvc.EditAsync(
                randomProjectId,
                randomTaskId,
                dto);

            await act.Should().ThrowAsync<NotFoundException>();

            mediator.Verify(m => m.Publish(
                    It.IsAny<TaskItemUpdated>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task MoveAsync_Returns_Updated_And_Publishes_Event_When_Moving_To_Different_Lane_And_Column()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, mediator, currentUser) = await CreateSutAsync(dbh);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            currentUser.UserId = user.Id;

            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var differentLane = TestDataFactory.SeedLane(db, projectId, order: 1);
            var differentColumn = TestDataFactory.SeedColumn(db, projectId, differentLane.Id);
            await db.SaveChangesAsync();

            var dto = new TaskItemMoveDto
            {
                NewLaneId = differentLane.Id,
                NewColumnId = differentColumn.Id,
                NewSortKey = 1
            };

            var moved = await writeSvc.MoveAsync(
                projectId,
                task.Id,
                dto);

            moved.LaneId.Should().Be(differentLane.Id);
            moved.ColumnId.Should().Be(differentColumn.Id);
            moved.SortKey.Should().Be(1m);

            mediator.Verify(m => m.Publish(
                    It.Is<TaskItemMoved>(n =>
                        n.ProjectId == projectId &&
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
            var (db, writeSvc, _, currentUser) = await CreateSutAsync(dbh);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            currentUser.UserId = user.Id;

            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var differentColumn = TestDataFactory.SeedColumn(db, projectId, laneId, order: 1);
            await db.SaveChangesAsync();

            var dto = new TaskItemMoveDto
            {
                NewLaneId = laneId,
                NewColumnId = differentColumn.Id,
                NewSortKey = 1
            };

            var moved = await writeSvc.MoveAsync(
                projectId,
                task.Id,
                dto);

            moved.LaneId.Should().Be(laneId);
            moved.ColumnId.Should().Be(differentColumn.Id);
        }

        [Fact]
        public async Task MoveAsync_Returns_Updated_When_Moving_To_Same_Lane_And_Column_But_Different_SortKey()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, _, currentUser) = await CreateSutAsync(dbh);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            currentUser.UserId = user.Id;

            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var dto = new TaskItemMoveDto
            {
                NewLaneId = laneId,
                NewColumnId = columnId,
                NewSortKey = 1
            };

            var moved = await writeSvc.MoveAsync(
                projectId,
                task.Id,
                dto);

            moved.SortKey.Should().Be(1m);
        }

        [Fact]
        public async Task MoveAsync_Allows_Moving_Within_Same_Position()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, mediator, currentUser) = await CreateSutAsync(dbh);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            currentUser.UserId = user.Id;

            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            var dto = new TaskItemMoveDto
            {
                NewLaneId = laneId,
                NewColumnId = columnId,
                NewSortKey = 0
            };

            var moved = await writeSvc.MoveAsync(
                projectId,
                task.Id,
                dto);

            moved.LaneId.Should().Be(laneId);
            moved.ColumnId.Should().Be(columnId);
            moved.SortKey.Should().Be(0m);

            mediator.Verify(m => m.Publish(
                    It.Is<TaskItemMoved>(n =>
                        n.ProjectId == projectId &&
                        n.Payload.TaskId == task.Id),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task MoveAsync_Throws_NotFound_When_Task_Does_Not_Exist()
        {
            using var dbh = new SqliteTestDb();
            var (_, writeSvc, mediator, currentUser) = await CreateSutAsync(dbh);

            var user = TestDataFactory.SeedUser(dbh.CreateContext());
            currentUser.UserId = user.Id;

            var randomProjectId = Guid.NewGuid();
            var randomTaskId = Guid.NewGuid();

            var dto = new TaskItemMoveDto
            {
                NewLaneId = Guid.NewGuid(),
                NewColumnId = Guid.NewGuid(),
                NewSortKey = 0
            };

            Func<Task> act = () => writeSvc.MoveAsync(randomProjectId, randomTaskId, dto);

            await act.Should().ThrowAsync<NotFoundException>();

            mediator.Verify(m => m.Publish(
                    It.IsAny<TaskItemMoved>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task MoveAsync_Throws_NotFound_When_Task_Does_Not_Exist_In_Other_Project()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, mediator, currentUser) = await CreateSutAsync(dbh);

            var (firstProjectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            currentUser.UserId = user.Id;

            var task = TestDataFactory.SeedTaskItem(db, firstProjectId, laneId, columnId);

            // Use a different project id and a random task id to ensure not found
            var otherProjectId = Guid.NewGuid();
            var randomTaskId = Guid.NewGuid();

            var dto = new TaskItemMoveDto
            {
                NewLaneId = laneId,
                NewColumnId = columnId,
                NewSortKey = 1
            };

            Func<Task> act = () => writeSvc.MoveAsync(
                otherProjectId,
                randomTaskId,
                dto);

            await act.Should().ThrowAsync<NotFoundException>();

            mediator.Verify(m => m.Publish(
                    It.IsAny<TaskItemMoved>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteByIdAsync_Removes_Task_And_Publishes_TaskDeleted()
        {
            using var dbh = new SqliteTestDb();
            var (db, writeSvc, mediator, currentUser) = await CreateSutAsync(dbh);

            var (projectId, laneId, columnId, _) = TestDataFactory.SeedLaneWithColumn(db);
            var user = TestDataFactory.SeedUser(db);
            currentUser.UserId = user.Id;

            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);

            mediator
                .Setup(m => m.Publish(
                        It.Is<TaskItemDeleted>(n =>
                            n.ProjectId == projectId &&
                            n.Payload.TaskId == task.Id),
                        It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await writeSvc.DeleteByIdAsync(projectId, task.Id);

            var any = await db.TaskItems.AnyAsync();
            any.Should().BeFalse();

            mediator.Verify(m => m.Publish(
                    It.Is<TaskItemDeleted>(n =>
                        n.ProjectId == projectId &&
                        n.Payload.TaskId == task.Id),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            mediator.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DeleteByIdAsync_Throws_NotFound_When_Random_Id()
        {
            using var dbh = new SqliteTestDb();
            var (_, writeSvc, mediator, currentUser) = await CreateSutAsync(dbh);

            var user = TestDataFactory.SeedUser(dbh.CreateContext());
            currentUser.UserId = user.Id;

            var randomProjectId = Guid.NewGuid();
            var randomTaskId = Guid.NewGuid();

            Func<Task> act = () => writeSvc.DeleteByIdAsync(
                randomProjectId,
                randomTaskId);

            await act.Should().ThrowAsync<NotFoundException>();

            mediator.Verify(m => m.Publish(
                    It.IsAny<INotification>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ---------- HELPERS ----------

        private static Task<(CollabTaskDbContext Db, TaskItemWriteService Service, Mock<IMediator> Mediator, FakeCurrentUserService CurrentUser)>
            CreateSutAsync(
                SqliteTestDb dbh,
                Guid? userId = null)
        {
            var db = dbh.CreateContext();
            var repo = new TaskItemRepository(db);
            var uow = new UnitOfWork(db);
            var activityRepo = new TaskActivityRepository(db);
            var activityWriteSvc = new TaskActivityWriteService(activityRepo, _clock);
            var currentUser = new FakeCurrentUserService
            {
                UserId = userId
            };
            var mediator = new Mock<IMediator>();

            var svc = new TaskItemWriteService(
                repo,
                uow,
                activityWriteSvc,
                currentUser,
                mediator.Object);

            return Task.FromResult((db, svc, mediator, currentUser));
        }
    }
}
