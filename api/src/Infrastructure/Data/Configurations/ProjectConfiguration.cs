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
                t.HasCheckConstraint("CK_Projects_UpdatedAt_GTE_CreatedAt", "[UpdatedAt] >= [CreatedAt]");

                t.HasCheckConstraint("CK_Projects_Slug_Lowercase", "[Slug] = LOWER([Slug])");
                t.HasCheckConstraint("CK_Projects_Slug_NoSpaces", "[Slug] NOT LIKE '% %'");
                t.HasCheckConstraint("CK_Projects_Slug_NoDoubleDash", "[Slug] NOT LIKE '%--%'");
                t.HasCheckConstraint("CK_Projects_Slug_NoLeadingDash", "[Slug] NOT LIKE '-%'");
                t.HasCheckConstraint("CK_Projects_Slug_NoTrailingDash", "[Slug] NOT LIKE '%-'");
            });

            e.HasKey(p => p.Id);

            e.HasIndex(p => p.Slug)
                .IsUnique();

            e.Property(p => p.Id)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            var nameConv = new ValueConverter<ProjectName, string>(
                toDb => toDb.Value,
                fromDb => ProjectName.Create(fromDb));

            e.Property(p => p.Name)
                .HasConversion(nameConv)
                .HasMaxLength(100)
                .IsRequired();

            var slugConv = new ValueConverter<ProjectSlug, string>(
                toDb => toDb.Value,
                fromDb => ProjectSlug.Create(fromDb));

            e.Property(p => p.Slug)
                .HasConversion(slugConv)
                .HasMaxLength(100)
                .IsRequired();

            e.Property(p => p.CreatedAt)
                .HasColumnType("datetimeoffset")
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .ValueGeneratedOnAdd();

            e.Property(p => p.UpdatedAt)
                .HasColumnType("datetimeoffset")
                .ValueGeneratedOnAdd();

            e.Property(p => p.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            e.HasMany(p => p.Members)
                .WithOne(pm => pm.Project)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
