using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Tests.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Tests.Persistence.Contracts
{
    [Collection("SqlServerContainer")]
    public sealed class TaskItemPersistenceContractTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;
        public TaskItemPersistenceContractTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

        private (ServiceProvider sp, AppDbContext db) BuildDb(string name)
        {
            var cs = $"{_baseCs};Database={name}";
            var sc = new ServiceCollection();
            sc.AddInfrastructure(cs);
            var sp = sc.BuildServiceProvider();
            return (sp, sp.GetRequiredService<AppDbContext>());
        }

        [Fact]
        public async Task Add_And_Read_Works()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var (pId, lId, cId) = TestHelpers.TestDataFactory.SeedLaneWithColumn(db);
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
            var (sp, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var (pId, lId, cId) = TestHelpers.TestDataFactory.SeedLaneWithColumn(db);
            var t = TestHelpers.TestDataFactory.SeedTaskItem(db, pId, lId, cId, TaskTitle.Create("Title A"));

            var stale = t.RowVersion!.ToArray();

            // first update
            t.Title = TaskTitle.Create("Title B");
            db.Entry(t).Property(x => x.Title).IsModified = true;
            await db.SaveChangesAsync();

            // second context with stale token tries to edit
            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.TaskItems.SingleAsync(x => x.Id == t.Id);
            db2.Entry(same).Property(x => x.RowVersion).OriginalValue = stale;
            same.Title = TaskTitle.Create("Other title");
            db2.Entry(same).Property(x => x.Title).IsModified = true;

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Delete()
        {
            var (sp, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var (pId, lId, cId) = TestHelpers.TestDataFactory.SeedLaneWithColumn(db);
            var t = TestHelpers.TestDataFactory.SeedTaskItem(db, pId, lId, cId);

            var stale = t.RowVersion!.ToArray();

            // mutate to bump RowVersion
            t.Description = TaskDescription.Create("upd");
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
