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
    public sealed class TaskNotePersistenceContractTests : IClassFixture<MsSqlContainerFixture>
    {
        private readonly string _baseCs;
        public TaskNotePersistenceContractTests(MsSqlContainerFixture fx) => _baseCs = fx.ContainerConnectionString;

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

            var (_, _, _, tId, _, uId) = TestHelpers.TestDataFactory.SeedFullBoard(db);
            var n = TaskNote.Create(tId, uId, NoteContent.Create("Note"));
            db.TaskNotes.Add(n);
            await db.SaveChangesAsync();

            var fromDb = await db.TaskNotes.AsNoTracking().SingleAsync(x => x.Id == n.Id);
            fromDb.Content.Value.Should().Be("Note");
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Update_And_Delete()
        {
            var (sp, db) = BuildDb($"ct_{Guid.NewGuid():N}");
            await db.Database.MigrateAsync();

            var (_, _, _, tId, _, uId) = TestHelpers.TestDataFactory.SeedFullBoard(db);
            var n = TaskNote.Create(tId, uId, NoteContent.Create("content A"));
            db.TaskNotes.Add(n);
            await db.SaveChangesAsync();

            var stale = n.RowVersion!.ToArray();

            // bump rowversion
            n.Content = NoteContent.Create("content B");
            db.Entry(n).Property(x => x.Content).IsModified = true;
            await db.SaveChangesAsync();

            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.TaskNotes.SingleAsync(x => x.Id == n.Id);

            // stale update
            db2.Entry(same).Property(x => x.RowVersion).OriginalValue = stale;
            same.Content = NoteContent.Create("oontent C");
            db2.Entry(same).Property(x => x.Content).IsModified = true;
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());

            // stale delete
            using var scope3 = sp.CreateScope();
            var db3 = scope3.ServiceProvider.GetRequiredService<AppDbContext>();
            var same2 = await db3.TaskNotes.SingleAsync(x => x.Id == n.Id);
            db3.Entry(same2).Property(x => x.RowVersion).OriginalValue = stale;
            db3.TaskNotes.Remove(same2);
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db3.SaveChangesAsync());
        }
    }
}
