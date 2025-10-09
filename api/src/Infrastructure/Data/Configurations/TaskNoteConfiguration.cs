using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Data.Configurations
{
    public sealed class TaskNoteConfiguration : IEntityTypeConfiguration<TaskNote>
    {
        public void Configure(EntityTypeBuilder<TaskNote> e)
        {
            e.ToTable("Notes", t =>
            {
                t.HasCheckConstraint("CK_Notes_Content_NotEmpty", "LEN(LTRIM(RTRIM([Content]))) > 0");
            });

            e.HasKey(n => n.Id);
            e.Property(n => n.Id).ValueGeneratedNever();

            e.Property(n => n.TaskId).IsRequired();
            e.Property(n => n.AuthorId).IsRequired();

            // Relationship: 1 Task - N Notes
            e.HasOne<TaskItem>()
                .WithMany()
                .HasForeignKey(n => n.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: 1 User - N Notes
            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.AuthorId)
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

            e.HasIndex(n => n.AuthorId)
                .HasDatabaseName("IX_Notes_AuthorId");
        }
    }
}
