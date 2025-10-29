using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestHelpers.Common;
using TestHelpers.Persistence;

namespace Infrastructure.Tests.Persistence
{
    [Collection("SqlServerContainer")]
    public sealed class TaskItemPersistenceTests(MsSqlContainerFixture fx)
    {
        private readonly MsSqlContainerFixture _fx = fx;
        private readonly string _cs = fx.ConnectionString;

        [Fact]
        public async Task Add_And_Read_Works()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var taskTitle = "Title";
            var taskDescription = "Description";
            var (projectId, laneId, columnId) = TestDataFactory.SeedLaneWithColumn(db);
            var task = TaskItem.Create(
                columnId,
                laneId,
                projectId,
                TaskTitle.Create(taskTitle),
                TaskDescription.Create(taskDescription));

            db.TaskItems.Add(task);
            await db.SaveChangesAsync();

            var fromDb = await db.TaskItems
                .AsNoTracking()
                .SingleAsync(t => t.Id == task.Id);
            fromDb.Title.Value.Should().Be(taskTitle);
            fromDb.Description.Value.Should().Be(taskDescription);
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Update()
        {
            await _fx.ResetAsync();
            var (sp, db) = DbHelper.BuildDb(_cs);

            var (projectId, laneId, columnId) = TestDataFactory.SeedLaneWithColumn(db);
            var task = TestDataFactory.SeedTaskItem(
                db,
                projectId,
                laneId,
                columnId,
                TaskTitle.Create("Title A"));
            var stale = task.RowVersion.ToArray();

            // First update
            task.Edit(
                title: TaskTitle.Create("Title B"),
                description: task.Description,
                dueDate: task.DueDate);
            db.Entry(task).Property(x => x.Title).IsModified = true;
            await db.SaveChangesAsync();

            // Second context with stale token tries to edit
            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.TaskItems.SingleAsync(t => t.Id == task.Id);

            db2.Entry(same).Property(x => x.RowVersion).OriginalValue = stale;
            same.Edit(TaskTitle.Create("Other title"), task.Description, task.DueDate);
            db2.Entry(same).Property(x => x.Title).IsModified = true;

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Delete()
        {
            await _fx.ResetAsync();
            var (sp, db) = DbHelper.BuildDb(_cs);

            var (projectId, laneId, columnId) = TestDataFactory.SeedLaneWithColumn(db);
            var task = TestDataFactory.SeedTaskItem(db, projectId, laneId, columnId);
            var stale = task.RowVersion.ToArray();

            // Mutate to bump RowVersion
            task.Edit(
                title: task.Title,
                description: TaskDescription.Create("upd"),
                dueDate: task.DueDate);
            db.Entry(task).Property(x => x.Description).IsModified = true;
            await db.SaveChangesAsync();

            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.TaskItems.SingleAsync(t => t.Id == task.Id);
            db2.Entry(same).Property(x => x.RowVersion).OriginalValue = stale;
            db2.TaskItems.Remove(same);

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        }
    }
}
