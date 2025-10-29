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
    public sealed class TaskNotePersistenceContractTests(MsSqlContainerFixture fx)
    {
        private readonly MsSqlContainerFixture _fx = fx;
        private readonly string _cs = fx.ConnectionString;

        [Fact]
        public async Task Add_And_Read_Works()
        {
            await _fx.ResetAsync();
            var (_, db) = DbHelper.BuildDb(_cs);

            var noteContent = "note";
            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);
            var note = TaskNote.Create(taskId, userId, NoteContent.Create(noteContent));
            db.TaskNotes.Add(note);
            await db.SaveChangesAsync();

            var fromDb = await db.TaskNotes
                .AsNoTracking()
                .SingleAsync(n => n.Id == note.Id);
            fromDb.Content.Value.Should().Be(noteContent);
        }

        [Fact]
        public async Task RowVersion_Concurrency_Throws_On_Stale_Update_And_Delete()
        {
            await _fx.ResetAsync();
            var (sp, db) = DbHelper.BuildDb(_cs);

            var (_, _, _, taskId, _, userId) = TestDataFactory.SeedFullBoard(db);
            var note = TaskNote.Create(taskId, userId, NoteContent.Create("content A"));

            db.TaskNotes.Add(note);
            await db.SaveChangesAsync();

            var stale = note.RowVersion!.ToArray();

            // bump rowversion
            note.Edit(NoteContent.Create("content B"));
            db.Entry(note).Property(x => x.Content).IsModified = true;
            await db.SaveChangesAsync();

            using var scope2 = sp.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            var same = await db2.TaskNotes.SingleAsync(n => n.Id == note.Id);

            // stale update
            db2.Entry(same).Property(x => x.RowVersion).OriginalValue = stale;
            same.Edit(NoteContent.Create("content C"));
            db2.Entry(same).Property(x => x.Content).IsModified = true;
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());

            // stale delete
            using var scope3 = sp.CreateScope();
            var db3 = scope3.ServiceProvider.GetRequiredService<AppDbContext>();
            var same2 = await db3.TaskNotes.SingleAsync(n => n.Id == note.Id);
            db3.Entry(same2).Property(x => x.RowVersion).OriginalValue = stale;
            db3.TaskNotes.Remove(same2);
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db3.SaveChangesAsync());
        }
    }
}
