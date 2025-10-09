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
            e.ToTable("Users", t =>
            {
                t.HasCheckConstraint("CK_Users_Email_NotEmpty", "LEN(LTRIM(RTRIM([Email]))) > 0");
                t.HasCheckConstraint("CK_Users_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                t.HasCheckConstraint("CK_Users_PasswordHash_Length_32", "DATALENGTH([PasswordHash]) = 32");
                t.HasCheckConstraint("CK_Users_PasswordSalt_Length_16", "DATALENGTH([PasswordSalt]) = 16");
            });

            e.HasKey(u => u.Id);
            e.Property(u => u.Id).ValueGeneratedNever();

            // Relationship: 1 User - N ProjectMemberships
            e.HasMany(u => u.ProjectMemberships)
                .WithOne(pm => pm.User)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // VOs
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

            // Password
            e.Property(u => u.PasswordHash)
                .HasColumnType("varbinary(32)")
                .IsRequired();

            e.Property(u => u.PasswordSalt)
                .HasColumnType("varbinary(16)")
                .IsRequired();

            // Role
            e.Property(u => u.Role)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            // Timestamps
            e.Property(u => u.CreatedAt).HasColumnType("datetimeoffset");
            e.Property(u => u.UpdatedAt).HasColumnType("datetimeoffset");

            // Concurrency token
            e.Property(u => u.RowVersion).IsRowVersion();

            // Indexes
            e.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("UX_Users_Email");

            e.HasIndex(u => u.Name)
                .IsUnique()
                .HasDatabaseName("IX_Users_Name");
        }
    }
}
