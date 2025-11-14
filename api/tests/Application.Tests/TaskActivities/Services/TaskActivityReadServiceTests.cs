using Application.TaskActivities.Services;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence.Repositories;
using TestHelpers.Common;
using TestHelpers.Persistence;

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
            var readSvc = new TaskActivityReadService(repo);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);
            var payload = "{\"k\":\"v\"}";
            var activity = TestDataFactory.SeedTaskActivity(
                db,
                taskId,
                userId,
                TaskActivityType.TaskCreated,
                payload);

            var found = await readSvc.GetAsync(activity.Id);
            found.Should().NotBeNull();
            found.Payload.Value.Should().Contain(payload);
        }

        [Fact]
        public async Task ListByTaskAsync_Returns_All_For_Task()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskActivityRepository(db);
            var readSvc = new TaskActivityReadService(repo);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);
            TestDataFactory.SeedTaskActivity(db, taskId, userId, TaskActivityType.TaskMoved, "{\"i\":1}");
            TestDataFactory.SeedTaskActivity(db, taskId, userId, TaskActivityType.TaskMoved, "{\"i\":2}");

            var list = await readSvc.ListByTaskAsync(taskId);
            list.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListByUserAsync_Returns_All_For_Actor()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskActivityRepository(db);
            var readSvc = new TaskActivityReadService(repo);

            var (_, _, _, taskId, _, userId1) = TestDataFactory.SeedFullBoard(db);
            var userId2 = TestDataFactory.SeedUser(db).Id;

            // use new assignment-related types
            TestDataFactory.SeedTaskActivity(db, taskId, userId1, TaskActivityType.AssignmentCreated, "{\"a\":1}");
            TestDataFactory.SeedTaskActivity(db, taskId, userId2, TaskActivityType.AssignmentRoleChanged, "{\"a\":2}");

            var list1 = await readSvc.ListByUserAsync(userId1);
            list1.Should().OnlyContain(a => a.ActorId == userId1);

            var list2 = await readSvc.ListByUserAsync(userId2);
            list2.Should().OnlyContain(a => a.ActorId == userId2);
        }

        [Fact]
        public async Task ListByTypeAsync_Filters_By_Type()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();
            var repo = new TaskActivityRepository(db);
            var readSvc = new TaskActivityReadService(repo);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);

            TestDataFactory.SeedTaskActivity(db, taskId, userId, TaskActivityType.TaskCreated, "{\"t\":\"c\"}");
            TestDataFactory.SeedTaskActivity(db, taskId, userId, TaskActivityType.NoteAdded, "{\"t\":\"m\"}");

            var onlyCreated = await readSvc.ListByTypeAsync(taskId, TaskActivityType.TaskCreated);
            onlyCreated.Should().ContainSingle();
            onlyCreated.Single().Type.Should().Be(TaskActivityType.TaskCreated);
        }
    }
}
