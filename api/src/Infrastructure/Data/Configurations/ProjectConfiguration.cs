using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Data.Configurations
{
    public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> e)
        {
            e.ToTable("Projects", t =>
            {
                t.HasCheckConstraint("CK_Projects_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                t.HasCheckConstraint("CK_Projects_UpdatedAt_GTE_CreatedAt", "[UpdatedAt] >= [CreatedAt]");
                t.HasCheckConstraint("CK_Projects_Slug_Lowercase", "[Slug] = LOWER([Slug])");
                t.HasCheckConstraint("CK_Projects_Slug_NoSpaces", "[Slug] NOT LIKE '% %'");
                t.HasCheckConstraint("CK_Projects_Slug_NoDoubleDash", "[Slug] NOT LIKE '%--%'");
                t.HasCheckConstraint("CK_Projects_Slug_NoLeadingDash", "[Slug] NOT LIKE '-%'");
                t.HasCheckConstraint("CK_Projects_Slug_NoTrailingDash", "[Slug] NOT LIKE '%-'");
            });

            e.HasKey(p => p.Id);
            e.Property(p => p.Id).ValueGeneratedNever();

            e.Property(p => p.OwnerId).IsRequired();

            // Relationship: 1 User - N Projects (Owner)
            e.HasOne<User>()
                .WithMany()
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship: 1 Project - N ProjectMembers
            e.HasMany(p => p.Members)
                .WithOne(pm => pm.Project)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // VOs
            var nameConversion = new ValueConverter<ProjectName, string>(
                toDb => toDb.Value,
                fromDb => ProjectName.Create(fromDb));

            e.Property(p => p.Name)
                .HasConversion(nameConversion)
                .HasMaxLength(100)
                .IsRequired();

            var slugConversion = new ValueConverter<ProjectSlug, string>(
                toDb => toDb.Value,
                fromDb => ProjectSlug.Create(fromDb));

            e.Property(p => p.Slug)
                .HasConversion(slugConversion)
                .HasMaxLength(100)
                .IsRequired();

            // Timestamps
            e.Property(p => p.CreatedAt).HasColumnType("datetimeoffset");
            e.Property(p => p.UpdatedAt).HasColumnType("datetimeoffset");

            // Concurrency token
            e.Property(p => p.RowVersion).IsRowVersion();

            // Indexes
            e.HasIndex(p => new { p.OwnerId, p.Slug })
                .IsUnique()
                .HasDatabaseName("UX_Projects_OwnerId_Slug");

            e.HasIndex(p => p.OwnerId)
                .HasDatabaseName("IX_Projects_OwnerId");
        }
    }
}
