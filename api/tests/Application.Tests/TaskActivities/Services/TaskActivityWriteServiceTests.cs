using Application.TaskActivities.Services;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data.Repositories;
using TestHelpers;

namespace Application.Tests.TaskActivities.Services
{
    public sealed class TaskActivityWriteServiceTests
    {
        [Fact]
        public async Task CreateAsync_Creates_And_Persists_Activity()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskActivityRepository(db);
            var svc = new TaskActivityWriteService(repo);

            var (_, _, _, taskId, _, actor) = TestDataFactory.SeedFullBoard(db);

            var (m, activity) = await svc.CreateAsync(taskId, actor, TaskActivityType.TaskCreated,
                ActivityPayload.Create("{\"event\":\"create\"}"));
            m.Should().Be(DomainMutation.Created);
            activity.Should().NotBeNull();

            var list = await repo.ListByTaskAsync(taskId);
            list.Should().ContainSingle();
        }

        [Fact]
        public async Task CreateManyAsync_NoOp_When_Empty()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskActivityRepository(db);
            var svc = new TaskActivityWriteService(repo);

            var res = await svc.CreateManyAsync([]);
            res.Should().Be(DomainMutation.NoOp);
        }

        [Fact]
        public async Task CreateManyAsync_Creates_All_Activities_In_Batch()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskActivityRepository(db);
            var svc = new TaskActivityWriteService(repo);

            var (_, _, _, taskId, _, actor) = TestDataFactory.SeedFullBoard(db);

            var batch = new List<(Guid TaskId, Guid ActorId, TaskActivityType Type, string Payload)>
        {
            (taskId, actor, TaskActivityType.TaskCreated, ActivityPayload.Create("{\"i\":1}")),
            (taskId, actor, TaskActivityType.NoteAdded, ActivityPayload.Create("{\"i\":2}")),
            (taskId, actor, TaskActivityType.TaskMoved, ActivityPayload.Create("{\"i\":3}"))
        };

            var res = await svc.CreateManyAsync(batch);
            res.Should().Be(DomainMutation.Created);

            var list = await repo.ListByTaskAsync(taskId);
            list.Should().HaveCount(3);
            list.Select(x => x.Type).Should().BeEquivalentTo(
                [TaskActivityType.TaskCreated, TaskActivityType.NoteAdded, TaskActivityType.TaskMoved]);
        }
    }
}
