using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class LaneConfiguration : IEntityTypeConfiguration<Lane>
    {
        public void Configure(EntityTypeBuilder<Lane> e)
        {
            e.ToTable("Lanes");

            e.HasKey(l => l.Id);
            e.Property(l => l.Id).ValueGeneratedNever();

            e.Property(l => l.ProjectId).IsRequired();

            // Relationship: 1 Project - N Lanes
            e.HasOne<Project>()
                .WithMany()
                .HasForeignKey(l => l.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // VO
            var nameConversion = new ValueConverter<LaneName, string>(
                toDb => toDb.Value,
                fromDb => LaneName.Create(fromDb));

            e.Property(l => l.Name)
                .HasConversion(nameConversion)
                .HasMaxLength(100)
                .IsRequired();

            // Order
            e.Property(l => l.Order).HasDefaultValue(0);

            // Concurrency token
            e.Property(l => l.RowVersion).IsRowVersion();

            // Indexes
            e.HasIndex(l => new { l.ProjectId, l.Name })
                .IsUnique()
                .HasDatabaseName("UX_Lanes_ProjectId_Name");

            e.HasIndex(l => new { l.ProjectId, l.Order })
                .IsUnique()
                .HasDatabaseName("UX_Lanes_ProjectId_Order");
        }
    }
}
