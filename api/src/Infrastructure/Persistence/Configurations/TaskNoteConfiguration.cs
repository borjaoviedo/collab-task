using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class TaskNoteConfiguration : IEntityTypeConfiguration<TaskNote>
    {
        public void Configure(EntityTypeBuilder<TaskNote> e)
        {
            e.ToTable("Notes");

            e.HasKey(n => n.Id);
            e.Property(n => n.Id).ValueGeneratedNever();

            e.Property(n => n.TaskId).IsRequired();
            e.Property(n => n.UserId).IsRequired();

            // Relationship: 1 Task - N Notes
            e.HasOne<TaskItem>()
                .WithMany()
                .HasForeignKey(n => n.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: 1 User - N Notes
            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // VO
            var contentConversion = new ValueConverter<NoteContent, string>(
                toDb => toDb.Value,
                fromDb => NoteContent.Create(fromDb));

            e.Property(n => n.Content)
                .HasConversion(contentConversion)
                .HasMaxLength(500)
                .IsRequired();

            // Timestamps
            e.Property(n => n.CreatedAt).HasColumnType("datetimeoffset");
            e.Property(n => n.UpdatedAt).HasColumnType("datetimeoffset");

            // Concurrency token
            e.Property(n => n.RowVersion).IsRowVersion();

            // Indexes
            e.HasIndex(n => new { n.TaskId, n.CreatedAt })
                .HasDatabaseName("IX_Notes_TaskId_CreatedAt");

            e.HasIndex(n => n.UserId)
                .HasDatabaseName("IX_Notes_UserId");
        }
    }
}
