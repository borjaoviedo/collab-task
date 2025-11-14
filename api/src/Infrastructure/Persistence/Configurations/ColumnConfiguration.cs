using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Persistence.Configurations
{
    public sealed class ColumnConfiguration : IEntityTypeConfiguration<Column>
    {
        public void Configure(EntityTypeBuilder<Column> e)
        {
            e.ToTable("Columns");

            e.HasKey(c => c.Id);
            e.Property(c => c.Id).ValueGeneratedNever();

            e.Property(c => c.LaneId).IsRequired();
            e.Property(c => c.ProjectId).IsRequired();

            // Relationship: 1 Lane - N Columns
            e.HasOne<Lane>()
                .WithMany()
                .HasForeignKey(c => c.LaneId)
                .OnDelete(DeleteBehavior.Cascade);

            // VO
            var nameConversion = new ValueConverter<ColumnName, string>(
                toDb => toDb.Value,
                fromDb => ColumnName.Create(fromDb));

            e.Property(c => c.Name)
                .HasConversion(nameConversion)
                .HasMaxLength(100)
                .IsRequired();

            // Order
            e.Property(c => c.Order).HasDefaultValue(0);

            // Concurrency token
            e.Property(c => c.RowVersion).IsRowVersion();

            // Indexes
            e.HasIndex(c => new { c.LaneId, c.Name })
                .IsUnique()
                .HasDatabaseName("UX_Columns_LaneId_Name");

            e.HasIndex(c => new { c.LaneId, c.Order })
                .IsUnique()
                .HasDatabaseName("UX_Columns_LaneId_Order");

            e.HasIndex(c => c.ProjectId)
                .HasDatabaseName("IX_Columns_ProjectId");
        }
    }
}
