using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
    {
        public void Configure(EntityTypeBuilder<ProjectMember> e)
        {
            e.ToTable("ProjectMembers", t =>
            {
                t.HasCheckConstraint("CK_ProjectMembers_Role", "[Role] IN (0,1,2,3)");
                t.HasCheckConstraint("CK_ProjectMembers_RemovedAt_After_JoinedAt",
                    "[RemovedAt] IS NULL OR [RemovedAt] >= [JoinedAt]");
                t.HasCheckConstraint("CK_ProjectMembers_InvitedAt_Before_JoinedAt",
                    "[InvitedAt] IS NULL OR [InvitedAt] <= [JoinedAt]");
            });

            e.HasKey(pm => new { pm.ProjectId, pm.UserId });

            e.HasIndex(pm => pm.UserId);
            e.HasIndex(pm => new { pm.ProjectId, pm.Role });
            e.HasIndex(pm => new { pm.ProjectId, pm.RemovedAt });

            e.HasIndex(pm => pm.ProjectId)
                 .HasFilter("[Role] = 0 AND [RemovedAt] IS NULL")
                 .IsUnique();

            e.Property(pm => pm.Role).IsRequired();

            e.Property(pm => pm.JoinedAt).HasColumnType("datetimeoffset");

            e.Property(pm => pm.InvitedAt)
                .HasColumnType("datetimeoffset")
                .IsRequired(false);

            e.Property(pm => pm.RemovedAt)
                .HasColumnType("datetimeoffset")
                .IsRequired(false);

            e.Property(pm => pm.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            e.HasOne(pm => pm.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMemberships)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
