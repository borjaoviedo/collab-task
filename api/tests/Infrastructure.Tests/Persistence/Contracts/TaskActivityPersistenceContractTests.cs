using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using TestHelpers;
using TestHelpers.Time;

namespace Infrastructure.Tests.Persistence.Contracts
{
    [Collection("SqlServerContainer")]
    public sealed class TaskActivityPersistenceContractTests(MsSqlContainerFixture fx)
    {
        private readonly MsSqlContainerFixture _fx = fx;
        private readonly string _cs = fx.ConnectionString;

        [Fact]
        public async Task Add_And_GetById_Works()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var (_, _, _, taskId, _, actorId) = TestDataFactory.SeedFullBoard(db);
            var activity = TaskActivity.Create(
                taskId,
                actorId,
                TaskActivityType.TaskCreated,
                ActivityPayload.Create("{\"e\":\"c\"}"),
                createdAt: TestTime.FixedNow);
            db.TaskActivities.Add(activity);
            await db.SaveChangesAsync();

            var found = await db.TaskActivities
                .AsNoTracking()
                .SingleAsync(a => a.Id == activity.Id);
            found.TaskId.Should().Be(taskId);
            found.ActorId.Should().Be(actorId);
            found.Type.Should().Be(TaskActivityType.TaskCreated);
        }

        [Fact]
        public async Task List_By_Task_Ordered_By_CreatedAt()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var (_, _, _, taskId, _, actor) = TestDataFactory.SeedFullBoard(db);

            var activity1 = TaskActivity.Create(
                taskId,
                actor,
                TaskActivityType.NoteAdded,
                ActivityPayload.Create("{\"m\":\"1\"}"),
                createdAt: TestTime.FromFixedMinutes(-3));
            var activity2 = TaskActivity.Create(
                taskId,
                actor,
                TaskActivityType.NoteEdited,
                ActivityPayload.Create("{\"m\":\"2\"}"),
                createdAt: TestTime.FromFixedMinutes(-2));
            var activity3 = TaskActivity.Create(
                taskId,
                actor,
                TaskActivityType.NoteRemoved,
                ActivityPayload.Create("{\"m\":\"3\"}"),
                createdAt: TestTime.FromFixedMinutes(-1));

            db.TaskActivities.AddRange(activity2, activity3, activity1);
            await db.SaveChangesAsync();

            var list = await db.TaskActivities
                                .AsNoTracking()
                                .Where(x => x.TaskId == taskId)
                                .OrderBy(x => x.CreatedAt)
                                .Select(x => x.Payload.Value)
                                .ToListAsync();

            list.Should().Equal("{\"m\":\"1\"}", "{\"m\":\"2\"}", "{\"m\":\"3\"}");
        }
    }
}
