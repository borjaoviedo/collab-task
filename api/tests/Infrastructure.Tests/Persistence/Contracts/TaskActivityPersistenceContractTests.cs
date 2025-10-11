using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Persistence.Contracts
{
    [Collection("SqlServerContainer")]
    public sealed class TaskActivityPersistenceContractTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;
        public TaskActivityPersistenceContractTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

        private (ServiceProvider sp, AppDbContext db) BuildDb(string name)
        {
            var cs = $"{_baseCs};Database={name}";
            var sc = new ServiceCollection();
            sc.AddInfrastructure(cs);
            var sp = sc.BuildServiceProvider();
            return (sp, sp.GetRequiredService<AppDbContext>());
        }

        [Fact]
        public async Task Add_And_GetById_Works()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var (_, _, _, taskId, _, actorId) = TestHelpers.TestDataFactory.SeedFullBoard(db);
            var a = TaskActivity.Create(taskId, actorId, TaskActivityType.TaskCreated, ActivityPayload.Create("{\"e\":\"c\"}"));
            db.TaskActivities.Add(a);
            await db.SaveChangesAsync();

            var found = await db.TaskActivities.AsNoTracking().SingleAsync(x => x.Id == a.Id);
            found.TaskId.Should().Be(taskId);
            found.ActorId.Should().Be(actorId);
            found.Type.Should().Be(TaskActivityType.TaskCreated);
        }

        [Fact]
        public async Task List_By_Task_Ordered_By_CreatedAt()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var (_, _, _, taskId, _, actor) = TestHelpers.TestDataFactory.SeedFullBoard(db);

            var a1 = TaskActivity.Create(taskId, actor, TaskActivityType.NoteAdded, ActivityPayload.Create("{\"m\":\"1\"}"));
            var a2 = TaskActivity.Create(taskId, actor, TaskActivityType.NoteEdited, ActivityPayload.Create("{\"m\":\"2\"}"));
            var a3 = TaskActivity.Create(taskId, actor, TaskActivityType.NoteRemoved, ActivityPayload.Create("{\"m\":\"3\"}"));

            a1.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-3);
            a2.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-2);
            a3.CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1);

            db.TaskActivities.AddRange(a2, a3, a1);
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
