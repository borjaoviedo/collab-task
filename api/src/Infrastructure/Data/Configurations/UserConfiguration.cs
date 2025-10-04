using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Data.Configurations
{
    public sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> e)
        {
            e.ToTable("Users");

            e.HasKey(u => u.Id);

            e.HasIndex(u => u.Email)
                .IsUnique();

            e.HasIndex(u => u.Name)
                .IsUnique();

            e.Property(u => u.Id)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            var emailConversion = new ValueConverter<Email, string>(
                toDb => toDb.Value,
                fromDb => Email.Create(fromDb));

            e.Property(u => u.Email)
                .HasConversion(emailConversion)
                .HasMaxLength(256)
                .IsRequired();

            var nameConversion = new ValueConverter<UserName, string>(
                toDb => toDb.Value,
                fromDb => UserName.Create(fromDb));

            e.Property(u => u.Name)
                .HasConversion(nameConversion)
                .HasMaxLength(100)
                .IsRequired();

            e.Property(u => u.PasswordHash)
                .HasMaxLength(32)
                .IsRequired();

            e.Property(u => u.PasswordSalt)
                .HasMaxLength(16)
                .IsRequired();

            e.Property(u => u.CreatedAt)
                .HasColumnType("datetimeoffset")
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .ValueGeneratedOnAdd();

            e.Property(u => u.UpdatedAt)
                .HasColumnType("datetimeoffset")
                .ValueGeneratedOnAdd();

            e.Property(u => u.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken();

            e.HasMany(u => u.ProjectMemberships)
                .WithOne(pm => pm.User)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
