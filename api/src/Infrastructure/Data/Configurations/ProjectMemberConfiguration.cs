using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
    {
        public void Configure(EntityTypeBuilder<ProjectMember> e)
        {
            e.ToTable("ProjectMembers");

            // Composite Primary Key
            e.HasKey(m => new { m.ProjectId, m.UserId });

            // Relationship: 1 Project - N Members
            e.HasOne(m => m.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship: 1 User - N ProjectMemberships
            e.HasOne(m => m.User)
                .WithMany(u => u.ProjectMemberships)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Role
            e.Property(m => m.Role)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(20);

            // Timestamps
            e.Property(m => m.JoinedAt)
                .HasColumnType("datetimeoffset")
                .IsRequired();

            e.Property(m => m.RemovedAt)
                .HasColumnType("datetimeoffset")
                .IsRequired(false);

            // Concurrency token
            e.Property(m => m.RowVersion).IsRowVersion();

            // Indexes
            e.HasIndex(pm => pm.UserId)
           .HasDatabaseName("IX_ProjectMembers_UserId");

            e.HasIndex(pm => new { pm.ProjectId, pm.Role })
                .HasDatabaseName("IX_ProjectMembers_ProjectId_Role");

            e.HasIndex(pm => new { pm.ProjectId, pm.RemovedAt })
                .HasDatabaseName("IX_ProjectMembers_ProjectId_RemovedAt");

            // Exactly one active Owner per project
            e.HasIndex(pm => pm.ProjectId)
                .IsUnique()
                .HasFilter("[Role] = 'Owner' AND [RemovedAt] IS NULL")
                .HasDatabaseName("UX_ProjectMembers_ProjectId_ActiveOwner");
        }
    }
}
