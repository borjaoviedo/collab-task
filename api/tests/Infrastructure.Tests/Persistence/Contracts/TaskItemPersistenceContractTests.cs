using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestHelpers;

namespace Infrastructure.Tests.Persistence.Contracts
{
    [Collection("SqlServerContainer")]
    public sealed class TaskItemPersistenceContractTests(MsSqlContainerFixture fx)
    {
        private readonly MsSqlContainerFixture _fx = fx;
        private readonly string _cs = fx.ConnectionString;

        [Fact]
        public async Task Add_And_Read_Works()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var t = TaskItem.Create(cId, lId, pId, TaskTitle.Create("Title"), TaskDescription.Create("Desc"));
            db.TaskItems.Add(t);
            await db.SaveChangesAsync();

            var fromDb = await db.TaskItems.AsNoTracking().SingleAsync(x => x.Id == t.Id);
            fromDb.Title.Value.Should().Be("Title");
            fromDb.Description.Value.Should().Be("Desc");
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Update()
        {
            await _fx.ResetAsync();
            var (sp, db) = DbHelper.BuildDb(_cs);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var t = TestDataFactory.SeedTaskItem(db, pId, lId, cId, TaskTitle.Create("Title A"));

            var stale = t.RowVersion!.ToArray();

            // first update
            t.Edit(
                title: TaskTitle.Create("Title B"),
                description: t.Description,
                dueDate: t.DueDate);
            db.Entry(t).Property(x => x.Title).IsModified = true;
            await db.SaveChangesAsync();

            // second context with stale token tries to edit
            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.TaskItems.SingleAsync(x => x.Id == t.Id);
            db2.Entry(same).Property(x => x.RowVersion).OriginalValue = stale;
            same.Edit(TaskTitle.Create("Other title"), description: t.Description, dueDate: t.DueDate);
            db2.Entry(same).Property(x => x.Title).IsModified = true;

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Delete()
        {
            await _fx.ResetAsync();
            var (sp, db) = DbHelper.BuildDb(_cs);

            var (pId, lId, cId) = TestDataFactory.SeedLaneWithColumn(db);
            var t = TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var stale = t.RowVersion!.ToArray();

            // mutate to bump RowVersion
            t.Edit(
                title: t.Title,
                description: TaskDescription.Create("upd"),
                dueDate: t.DueDate);
            db.Entry(t).Property(x => x.Description).IsModified = true;
            await db.SaveChangesAsync();

            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.TaskItems.SingleAsync(x => x.Id == t.Id);
            db2.Entry(same).Property(x => x.RowVersion).OriginalValue = stale;
            db2.TaskItems.Remove(same);

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        }
    }
}
