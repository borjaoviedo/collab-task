using Application.Common.Abstractions.Time;
using Application.TaskActivities.Services;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;
using TestHelpers;
using TestHelpers.Time;

namespace Application.Tests.TaskActivities.Services
{
    public sealed class TaskActivityWriteServiceTests
    {
        private readonly IDateTimeProvider _clock = TestTime.FixedClock();

        [Fact]
        public async Task CreateAsync_Creates_And_Persists_Activity()
        {
            using var dbh = new SqliteTestDb();
            await using var db = dbh.CreateContext();

            var repo = new TaskActivityRepository(db);
            var uow = new UnitOfWork(db);
            var writeSvc = new TaskActivityWriteService(repo, _clock);

            var (_, _, _, taskId, _, actor) = TestDataFactory.SeedFullBoard(db);

            var (result, activity) = await writeSvc.CreateAsync(
                taskId,
                actor,
                TaskActivityType.TaskCreated,
                ActivityPayload.Create("{\"event\":\"create\"}")
            );

            result.Should().Be(DomainMutation.Created);
            activity.Should().NotBeNull();

            await uow.SaveAsync(MutationKind.Create);

            var list = await repo.ListByTaskAsync(taskId);
            list.Should().ContainSingle();
        }
    }
}
