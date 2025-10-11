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
    public sealed class ColumnPersistenceContractTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;
        public ColumnPersistenceContractTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

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

            var (pId, lId) = TestHelpers.TestDataFactory.SeedProjectWithLane(db);
            var c = Column.Create(pId, lId, ColumnName.Create("To Do"), 0);
            db.Columns.Add(c);
            await db.SaveChangesAsync();

            var fromDb = await db.Columns.AsNoTracking().SingleAsync(x => x.Id == c.Id);
            fromDb.Name.Value.Should().Be("To Do");
            fromDb.Order.Should().Be(0);
        }

        [Fact]
        public async Task Unique_Index_LaneId_Name_Is_Enforced()
        {
            var (_, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var (pId, lId) = TestHelpers.TestDataFactory.SeedProjectWithLane(db);
            var name = ColumnName.Create("Same");
            db.Columns.Add(Column.Create(pId, lId, name, 0));
            await db.SaveChangesAsync();

            var dup = Column.Create(pId, lId, name, 1);
            db.Columns.Add(dup);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            db.Entry(dup).State = EntityState.Detached;

            // same name in other lane is allowed
            var l2 = TestHelpers.TestDataFactory.SeedLane(db, pId, order: 1);
            db.Columns.Add(Column.Create(pId, l2.Id, name, 0));
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Rename()
        {
            var (sp, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var (pId, lId) = TestHelpers.TestDataFactory.SeedProjectWithLane(db);
            var c = TestHelpers.TestDataFactory.SeedColumn(db, pId, lId, "column A", 0);

            var stale = c.RowVersion!.ToArray();

            c.Name = ColumnName.Create("column B");
            db.Entry(c).Property(x => x.Name).IsModified = true;
            await db.SaveChangesAsync();

            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.Columns.SingleAsync(x => x.Id == c.Id);
            db2.Entry(same).Property(x => x.RowVersion).OriginalValue = stale;
            same.Name = ColumnName.Create("column C");
            db2.Entry(same).Property(x => x.Name).IsModified = true;

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
        }
    }
}
