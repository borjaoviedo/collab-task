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

            var (m, activity) = await svc.CreateAsync(
                taskId,
                actor,
                TaskActivityType.TaskCreated,
                ActivityPayload.Create("{\"event\":\"create\"}")
            );

            m.Should().Be(DomainMutation.Created);
            activity.Should().NotBeNull();

            await repo.SaveChangesAsync();

            var list = await repo.ListByTaskAsync(taskId);
            list.Should().ContainSingle();
        }
    }
}
