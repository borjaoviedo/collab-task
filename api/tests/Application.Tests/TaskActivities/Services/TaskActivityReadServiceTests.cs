using Application.TaskActivities.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using TestHelpers;

namespace Application.Tests.TaskActivities.Services
{
    public sealed class TaskActivityReadServiceTests
    {
        [Fact]
        public async Task GetAsync_Returns_Activity_By_Id()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskActivityRepository(db);
            var svc = new TaskActivityReadService(repo);

            var (_, _, _, taskId, _, actor) = TestDataFactory.SeedFullBoard(db);
            var payload = "{\"k\":\"v\"}";
            var activity = TestDataFactory.SeedTaskActivity(db, taskId, actor, TaskActivityType.TaskCreated, payload);

            var found = await svc.GetAsync(activity.Id);
            found.Should().NotBeNull();
            found!.Payload.Value.Should().Contain(payload);
        }

        [Fact]
        public async Task ListByTaskAsync_Returns_All_For_Task()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskActivityRepository(db);
            var svc = new TaskActivityReadService(repo);

            var (_, _, _, taskId, _, actor) = TestDataFactory.SeedFullBoard(db);
            TestDataFactory.SeedTaskActivity(db, taskId, actor, TaskActivityType.TaskMoved, "{\"i\":1}");
            TestDataFactory.SeedTaskActivity(db, taskId, actor, TaskActivityType.TaskMoved, "{\"i\":2}");

            var list = await svc.ListByTaskAsync(taskId);
            list.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListByActorAsync_Returns_All_For_Actor()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskActivityRepository(db);
            var svc = new TaskActivityReadService(repo);

            var (_, _, _, taskId, _, actor1) = TestDataFactory.SeedFullBoard(db);
            var actor2 = TestDataFactory.SeedUser(db).Id;

            // use new assignment-related types
            TestDataFactory.SeedTaskActivity(db, taskId, actor1, TaskActivityType.AssignmentCreated, "{\"a\":1}");
            TestDataFactory.SeedTaskActivity(db, taskId, actor2, TaskActivityType.AssignmentRoleChanged, "{\"a\":2}");

            var list1 = await svc.ListByActorAsync(actor1);
            list1.Should().OnlyContain(x => x.ActorId == actor1);

            var list2 = await svc.ListByActorAsync(actor2);
            list2.Should().OnlyContain(x => x.ActorId == actor2);
        }

        [Fact]
        public async Task ListByTypeAsync_Filters_By_Type()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskActivityRepository(db);
            var svc = new TaskActivityReadService(repo);

            var (_, _, _, taskId, _, actor) = TestDataFactory.SeedFullBoard(db);

            TestDataFactory.SeedTaskActivity(db, taskId, actor, TaskActivityType.TaskCreated, "{\"t\":\"c\"}");
            TestDataFactory.SeedTaskActivity(db, taskId, actor, TaskActivityType.NoteAdded, "{\"t\":\"m\"}");

            var onlyCreated = await svc.ListByTypeAsync(taskId, TaskActivityType.TaskCreated);
            onlyCreated.Should().ContainSingle();
            onlyCreated.Single().Type.Should().Be(TaskActivityType.TaskCreated);
        }
    }
}
