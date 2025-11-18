using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using TestHelpers.Common;
using TestHelpers.Common.Testing;
using TestHelpers.Common.Time;
using TestHelpers.Persistence;

namespace Infrastructure.Tests.Repositories
{
    [IntegrationTest]
    public sealed class TaskActivityRepositoryTests
    {
        [Fact]
        public async Task AddRangeAsync_And_ListByTaskIdAsync_Returns_Ordered_By_CreatedAt()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskActivityRepository(db);
            var uow = new UnitOfWork(db);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);

            var payload1 = "{\"msg\":\"activity1\"}";
            var payload2 = "{\"msg\":\"activity2\"}";
            var payload3 = "{\"msg\":\"activity3\"}";
            var activity1 = TaskActivity.Create(
                taskId,
                userId,
                TaskActivityType.NoteAdded,
                ActivityPayload.Create(payload1),
                createdAt: TestTime.FromFixedMinutes(-3));

            await repo.AddAsync(activity1);
            await uow.SaveAsync(MutationKind.Create);

            var activity2 = TaskActivity.Create
                (taskId,
                userId,
                TaskActivityType.NoteEdited,
                ActivityPayload.Create(payload2),
                createdAt: TestTime.FromFixedMinutes(-2));

            await repo.AddAsync(activity2);
            await uow.SaveAsync(MutationKind.Create);

            var activity3 = TaskActivity.Create(
                taskId,
                userId,
                TaskActivityType.NoteRemoved,
                ActivityPayload.Create(payload3),
                createdAt: TestTime.FromFixedMinutes(-1));

            await repo.AddAsync(activity3);
            await uow.SaveAsync(MutationKind.Create);

            var list = await repo.ListByTaskIdAsync(taskId);
            list.Select(a => a.Payload.Value).Should().Equal(payload1, payload2, payload3);
        }

        [Fact]
        public async Task ListByUserIdAsync_Filters_By_Actor()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskActivityRepository(db);
            var uow = new UnitOfWork(db);

            var (_, _ , _, taskId, _, userId1) = TestDataFactory.SeedFullBoard(db);
            var userId2 = TestDataFactory.SeedUser(db).Id;

            var payload1 = "{\"a\":1}";
            var payload2 = "{\"a\":2}";
            var activity1 = TaskActivity.Create(
                taskId,
                userId1,
                TaskActivityType.NoteAdded,
                ActivityPayload.Create(payload1),
                createdAt: TestTime.FixedNow);

            await repo.AddAsync(activity1);
            await uow.SaveAsync(MutationKind.Create);

            var activity2 = TaskActivity.Create(
                taskId,
                userId2,
                TaskActivityType.NoteEdited,
                ActivityPayload.Create(payload2),
                createdAt: TestTime.FixedNow);

            await repo.AddAsync(activity2);
            await uow.SaveAsync(MutationKind.Create);

            var list1 = await repo.ListByUserIdAsync(userId1);
            list1.Should().ContainSingle(a => a.ActorId == userId1);

            var list2 = await repo.ListByUserIdAsync(userId2);
            list2.Should().ContainSingle(a => a.ActorId == userId2);
        }

        [Fact]
        public async Task ListByTaskTypeAsync_Filters_By_Type_Within_Task()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskActivityRepository(db);
            var uow = new UnitOfWork(db);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);

            var created = TaskActivity.Create(
                taskId,
                userId,
                TaskActivityType.TaskCreated,
                ActivityPayload.Create("{\"e\":\"c\"}"),
                createdAt: TestTime.FixedNow);

            await repo.AddAsync(created);
            await uow.SaveAsync(MutationKind.Create);

            var noteAdded = TaskActivity.Create(
                taskId,
                userId,
                TaskActivityType.NoteAdded,
                ActivityPayload.Create("{\"e\":\"m\"}"),
                createdAt: TestTime.FixedNow);

            await repo.AddAsync(noteAdded);
            await uow.SaveAsync(MutationKind.Create);

            var onlyComments = await repo.ListByTaskTypeAsync(taskId, TaskActivityType.TaskCreated);
            onlyComments.Should().ContainSingle();
            onlyComments.Single().Type.Should().Be(TaskActivityType.TaskCreated);
        }

        // --------------- Add ---------------

        [Fact]
        public async Task AddAsync_And_GetByIdAsync_Persist_And_Read()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskActivityRepository(db);
            var uow = new UnitOfWork(db);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);

            var activity = TaskActivity.Create(
                taskId,
                userId,
                TaskActivityType.TaskCreated,
                ActivityPayload.Create("{\"event\":\"created\"}"),
                createdAt: TestTime.FixedNow);

            await repo.AddAsync(activity);
            await uow.SaveAsync(MutationKind.Create);

            var found = await repo.GetByIdAsync(activity.Id);
            found.Should().NotBeNull();
            found.TaskId.Should().Be(taskId);
            found.ActorId.Should().Be(userId);
        }
    }
}
