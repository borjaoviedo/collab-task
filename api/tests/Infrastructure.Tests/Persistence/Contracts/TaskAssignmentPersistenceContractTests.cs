using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Persistence.Contracts
{
    [Collection("SqlServerContainer")]
    public sealed class TaskAssignmentPersistenceContractTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;
        public TaskAssignmentPersistenceContractTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

        private (ServiceProvider sp, AppDbContext db) BuildDb(string name)
        {
            var cs = $"{_baseCs};Database={name}";
            var sc = new ServiceCollection();
            sc.AddInfrastructure(cs);
            var sp = sc.BuildServiceProvider();
            return (sp, sp.GetRequiredService<AppDbContext>());
        }

        [Fact]
        public async Task Add_And_Get_Works()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var (_, userId) = TestHelpers.TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId) = TestHelpers.TestDataFactory.SeedColumnWithTask(db);

            var a = TaskAssignment.Create(taskId, userId, TaskRole.Owner);
            db.TaskAssignments.Add(a);
            await db.SaveChangesAsync();

            var fromDb = await db.TaskAssignments.AsNoTracking()
                .SingleAsync(x => x.TaskId == taskId && x.UserId == userId);
            fromDb.Role.Should().Be(TaskRole.Owner);
        }

        [Fact]
        public async Task Unique_Index_TaskId_UserId_Is_Enforced()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var (_, userId) = TestHelpers.TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId) = TestHelpers.TestDataFactory.SeedColumnWithTask(db);

            db.TaskAssignments.Add(TaskAssignment.Create(taskId, userId, TaskRole.Owner));
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();

            var dup = TaskAssignment.Create(taskId, userId, TaskRole.CoOwner);
            db.TaskAssignments.Add(dup);

            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.Entry(dup).State = EntityState.Detached; // prevent retry

            // sanity: a different user is allowed
            var otherUser = TestHelpers.TestDataFactory.SeedUser(db).Id;
            db.TaskAssignments.Add(TaskAssignment.Create(taskId, otherUser, TaskRole.CoOwner));
            await db.SaveChangesAsync();

            var count = await db.TaskAssignments.CountAsync(x => x.TaskId == taskId);
            count.Should().Be(2);
        }
    }
}
