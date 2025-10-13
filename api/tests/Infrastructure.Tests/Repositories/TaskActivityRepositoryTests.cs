using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using TestHelpers;

namespace Infrastructure.Tests.Repositories
{
    public sealed class TaskActivityRepositoryTests
    {
        [Fact]
        public async Task AddAsync_And_GetByIdAsync_Persist_And_Read()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new TaskActivityRepository(db);

            var (_, _, _, taskId, _, actorId) = TestDataFactory.SeedFullBoard(db);

            var activity = TaskActivity.Create(taskId, actorId, TaskActivityType.TaskCreated,
                ActivityPayload.Create("{\"event\":\"created\"}"));

            await repo.AddAsync(activity);
            await repo.SaveChangesAsync();

            var found = await repo.GetByIdAsync(activity.Id);
            found.Should().NotBeNull();
            found!.TaskId.Should().Be(taskId);
            found.ActorId.Should().Be(actorId);
        }

        [Fact]
        public async Task AddRangeAsync_And_ListByTaskAsync_Returns_Ordered_By_CreatedAt()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new TaskActivityRepository(db);

            var (_, _, _, taskId, _, actorId) = TestDataFactory.SeedFullBoard(db);

            var payload1 = "{\"msg\":\"a1\"}";
            var payload2 = "{\"msg\":\"a2\"}";
            var payload3 = "{\"msg\":\"a3\"}";
            var a1 = TaskActivity.Create(taskId, actorId, TaskActivityType.NoteAdded,
                ActivityPayload.Create(payload1));
            var a2 = TaskActivity.Create(taskId, actorId, TaskActivityType.NoteEdited,
                ActivityPayload.Create(payload2));
            var a3 = TaskActivity.Create(taskId, actorId, TaskActivityType.NoteRemoved,
                ActivityPayload.Create(payload3));

            a1.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-3);
            a2.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-2);
            a3.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1);

            await repo.AddRangeAsync([a2, a3, a1]);
            await repo.SaveChangesAsync();

            var list = await repo.ListByTaskAsync(taskId);
            list.Select(x => x.Payload.Value).Should().Equal(payload1, payload2, payload3);
        }

        [Fact]
        public async Task ListByActorAsync_Filters_By_Actor()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new TaskActivityRepository(db);

            var (_, _ , _, taskId, _, actor1) = TestDataFactory.SeedFullBoard(db);
            var actor2 = TestDataFactory.SeedUser(db).Id;

            var payload1 = "{\"a\":1}";
            var payload2 = "{\"a\":2}";
            var a1 = TaskActivity.Create(taskId, actor1, TaskActivityType.NoteAdded, ActivityPayload.Create(payload1));
            var a2 = TaskActivity.Create(taskId, actor2, TaskActivityType.NoteEdited, ActivityPayload.Create(payload2));
            await repo.AddRangeAsync([a1, a2]);
            await repo.SaveChangesAsync();

            var list1 = await repo.ListByActorAsync(actor1);
            list1.Should().ContainSingle(x => x.ActorId == actor1);

            var list2 = await repo.ListByActorAsync(actor2);
            list2.Should().ContainSingle(x => x.ActorId == actor2);
        }

        [Fact]
        public async Task ListByTypeAsync_Filters_By_Type_Within_Task()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new TaskActivityRepository(db);

            var (_, _, _, taskId, _, actor) = TestDataFactory.SeedFullBoard(db);

            var created = TaskActivity.Create(taskId, actor, TaskActivityType.TaskCreated, ActivityPayload.Create("{\"e\":\"c\"}"));
            var noteAdded = TaskActivity.Create(taskId, actor, TaskActivityType.NoteAdded, ActivityPayload.Create("{\"e\":\"m\"}"));
            await repo.AddRangeAsync([created, noteAdded]);
            await repo.SaveChangesAsync();

            var onlyComments = await repo.ListByTypeAsync(taskId, TaskActivityType.TaskCreated);
            onlyComments.Should().ContainSingle();
            onlyComments.Single().Type.Should().Be(TaskActivityType.TaskCreated);
        }

        [Fact]
        public async Task SaveChangesAsync_Returns_Affected_Count()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext(recreate: true);
            var repo = new TaskActivityRepository(db);

            var (_, _, _, taskId, _, actor) = TestDataFactory.SeedFullBoard(db);
            var a = TaskActivity.Create(taskId, actor, TaskActivityType.TaskCreated, ActivityPayload.Create("{\"x\":1}"));
            await repo.AddAsync(a);
            var affected = await repo.SaveChangesAsync();
            affected.Should().BeGreaterThan(0);
        }
    }
}
