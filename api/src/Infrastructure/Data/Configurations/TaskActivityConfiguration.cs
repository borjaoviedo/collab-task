using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Data.Configurations
{
    public sealed class TaskActivityConfiguration : IEntityTypeConfiguration<TaskActivity>
    {
        public void Configure(EntityTypeBuilder<TaskActivity> e)
        {
            e.ToTable("TaskActivities");

            e.HasKey(a => a.Id);
            e.Property(a => a.Id).ValueGeneratedNever();

            e.Property(a => a.TaskId).IsRequired();
            e.Property(a => a.ActorId).IsRequired();

            e.Property(a => a.Type)
                .HasConversion<string>()
                .HasMaxLength(40)
                .IsRequired();

            // Relationship: 1 Task - N Activities
            e.HasOne<TaskItem>()
                .WithMany()
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: 1 User - N Activities
            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.ActorId)
                .OnDelete(DeleteBehavior.Restrict);

            // VO
            var payloadConversion = new ValueConverter<ActivityPayload, string>(
                toDb => toDb.Value,
                fromDb => ActivityPayload.Create(fromDb));

            e.Property(a => a.Payload)
                .HasConversion(payloadConversion)
                .IsRequired();

            // Timestamp
            e.Property(a => a.CreatedAt)
                .HasColumnType("datetimeoffset")
                .IsRequired();

            // Indexes
            e.HasIndex(a => new { a.TaskId, a.CreatedAt })
                .HasDatabaseName("IX_TaskActivities_TaskId_CreatedAt");

            e.HasIndex(a => a.ActorId)
                .HasDatabaseName("IX_TaskActivities_ActorId");
        }
    }
}
