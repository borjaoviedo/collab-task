using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using TestHelpers;

namespace Infrastructure.Tests.Persistence.Contracts
{
    [Collection("SqlServerContainer")]
    public sealed class TaskAssignmentPersistenceContractTests(MsSqlContainerFixture fx)
    {
        private readonly MsSqlContainerFixture _fx = fx;
        private readonly string _cs = fx.ConnectionString;

        [Fact]
        public async Task Add_And_Get_Works()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

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
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var (_, userId) = TestDataFactory.SeedUserWithProject(db);
            var (_, _, _, taskId) = TestDataFactory.SeedColumnWithTask(db);

            db.TaskAssignments.Add(TaskAssignment.Create(taskId, userId, TaskRole.Owner));
            await db.SaveChangesAsync();

            db.ChangeTracker.Clear();

            var dup = TaskAssignment.Create(taskId, userId, TaskRole.CoOwner);
            db.TaskAssignments.Add(dup);

            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.Entry(dup).State = EntityState.Detached; // prevent retry

            // sanity: a different user is allowed
            var otherUser = TestDataFactory.SeedUser(db).Id;
            db.TaskAssignments.Add(TaskAssignment.Create(taskId, otherUser, TaskRole.CoOwner));
            await db.SaveChangesAsync();

            var count = await db.TaskAssignments.CountAsync(x => x.TaskId == taskId);
            count.Should().Be(2);
        }
    }
}
