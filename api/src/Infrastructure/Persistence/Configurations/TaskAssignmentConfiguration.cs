using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class TaskAssignmentConfiguration : IEntityTypeConfiguration<TaskAssignment>
    {
        public void Configure(EntityTypeBuilder<TaskAssignment> e)
        {
            e.ToTable("Assignments");

            // Composite Primary Key
            e.HasKey(a => new { a.TaskId, a.UserId });

            e.Property(a => a.TaskId).IsRequired();
            e.Property(a => a.UserId).IsRequired();

            e.Property(a => a.Role)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            // Relationship: 1 Task - N Assignments
            e.HasOne<TaskItem>()
                .WithMany()
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: 1 User - N Assignments
            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Concurrency token
            e.Property(a => a.RowVersion).IsRowVersion();

            // Indexes
            e.HasIndex(a => a.TaskId)
                .HasDatabaseName("IX_Assignments_TaskId");

            e.HasIndex(a => a.UserId)
                .HasDatabaseName("IX_Assignments_UserId");
        }
    }
}
