using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
    {
        public void Configure(EntityTypeBuilder<TaskItem> e)
        {
            e.ToTable("Tasks");

            e.HasKey(t => t.Id);
            e.Property(t => t.Id).ValueGeneratedNever();

            e.Property(t => t.ColumnId).IsRequired();
            e.Property(t => t.LaneId).IsRequired();
            e.Property(t => t.ProjectId).IsRequired();

            // Relationship: 1 Column - N Tasks
            e.HasOne<Column>()
                .WithMany()
                .HasForeignKey(t => t.ColumnId)
                .OnDelete(DeleteBehavior.Cascade);

            // VOs
            var titleConversion = new ValueConverter<TaskTitle, string>(
                toDb => toDb.Value,
                fromDb => TaskTitle.Create(fromDb));

            e.Property(t => t.Title)
                .HasConversion(titleConversion)
                .HasMaxLength(100)
                .IsRequired();

            var descriptionConversion = new ValueConverter<TaskDescription, string>(
                toDb => toDb.Value,
                fromDb => TaskDescription.Create(fromDb));

            e.Property(t => t.Description)
                .HasConversion(descriptionConversion)
                .HasMaxLength(2000)
                .IsRequired();

            // Timestamps
            e.Property(t => t.CreatedAt).HasColumnType("datetimeoffset");
            e.Property(t => t.UpdatedAt).HasColumnType("datetimeoffset");
            e.Property(t => t.DueDate)
                .HasColumnType("datetimeoffset")
                .IsRequired(false);

            // SortKey
            e.Property(t => t.SortKey)
                .HasColumnType("decimal(18,6)")
                .HasDefaultValue(0m)
                .IsRequired();

            // Concurrency token
            e.Property(t => t.RowVersion).IsRowVersion();

            // Indexes
            e.HasIndex(t => new { t.ColumnId, t.SortKey })
                .HasDatabaseName("IX_Tasks_ColumnId_SortKey");

            e.HasIndex(t => t.ProjectId)
                .HasDatabaseName("IX_Tasks_ProjectId");

            e.HasIndex(t => t.LaneId)
                .HasDatabaseName("IX_Tasks_LaneId");
        }
    }
}
