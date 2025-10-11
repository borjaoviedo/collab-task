using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Data
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<Lane> Lanes { get; set; }
        public DbSet<Column> Columns { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<TaskNote> TaskNotes { get; set; }
        public DbSet<TaskActivity> TaskActivities { get; set; }
        public DbSet<TaskAssignment> TaskAssignments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            ConfigureProviderSpecificMappings(modelBuilder);
        }

        private void ConfigureProviderSpecificMappings(ModelBuilder modelBuilder)
        {
            if (Database.IsSqlServer())
            {
                // Columns
                modelBuilder.Entity<Column>(e =>
                {
                    e.ToTable("Columns", t =>
                    {
                        t.HasCheckConstraint("CK_Columns_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                        t.HasCheckConstraint("CK_Columns_Order_NonNegative", "[Order] >= 0");
                    });
                });

                // Lanes
                modelBuilder.Entity<Lane>(e =>
                {
                    e.ToTable("Lanes", t =>
                    {
                        t.HasCheckConstraint("CK_Lanes_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                        t.HasCheckConstraint("CK_Lanes_Order_NonNegative", "[Order] >= 0");


                    });
                });

                // Projects
                modelBuilder.Entity<Project>(e =>
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
                });

                // ProjectMembers
                modelBuilder.Entity<ProjectMember>(e =>
                {
                    e.ToTable("ProjectMembers", t =>
                    {
                        t.HasCheckConstraint(
                            "CK_ProjectMembers_RemovedAt_After_JoinedAt",
                            "[RemovedAt] IS NULL OR [RemovedAt] >= [JoinedAt]");
                    });
                });

                // TaskActivities
                modelBuilder.Entity<TaskActivity>(e =>
                {
                    e.ToTable("TaskActivities", t =>
                    {
                        t.HasCheckConstraint("CK_TaskActivities_Payload_NotEmpty", "LEN(LTRIM(RTRIM([Payload]))) > 0");
                    });
                });

                // Tasks (TaskItem)
                modelBuilder.Entity<TaskItem>(e =>
                {
                    e.ToTable("Tasks", t =>
                    {
                        t.HasCheckConstraint("CK_Tasks_Title_NotEmpty", "LEN(LTRIM(RTRIM([Title]))) > 0");
                        t.HasCheckConstraint("CK_Tasks_Description_NotEmpty", "LEN(LTRIM(RTRIM([Description]))) > 0");
                        t.HasCheckConstraint("CK_Tasks_SortKey_NonNegative", "[SortKey] >= 0");
                    });
                });

                // Notes (TaskNote)
                modelBuilder.Entity<TaskNote>(e =>
                {
                    e.ToTable("Notes", t =>
                    {
                        t.HasCheckConstraint("CK_Notes_Content_NotEmpty", "LEN(LTRIM(RTRIM([Content]))) > 0");
                    });
                });

                // Users
                modelBuilder.Entity<User>(e =>
                {
                    e.ToTable("Users", t =>
                    {
                        t.HasCheckConstraint("CK_Users_Email_NotEmpty", "LEN(LTRIM(RTRIM([Email]))) > 0");
                        t.HasCheckConstraint("CK_Users_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                        t.HasCheckConstraint("CK_Users_PasswordHash_Length_32", "DATALENGTH([PasswordHash]) = 32");
                        t.HasCheckConstraint("CK_Users_PasswordSalt_Length_16", "DATALENGTH([PasswordSalt]) = 16");
                    });
                });

                return;
            }

            // SQLite
            modelBuilder.Entity<Column>(e =>
            {
                e.ToTable("Columns", t =>
                {
                    t.HasCheckConstraint("CK_Columns_Name_NotEmpty_sqlite", "length(trim(Name)) > 0");
                    t.HasCheckConstraint("CK_Columns_Order_NonNegative_sqlite", "[Order] >= 0");
                });

                e.Property(c => c.RowVersion)
                    .IsRequired()
                    .IsConcurrencyToken()
                    .ValueGeneratedOnAddOrUpdate()
                    .HasDefaultValueSql("randomblob(8)");
            });

            modelBuilder.Entity<Lane>(e =>
            {
                e.ToTable("Lanes", t =>
                {
                    t.HasCheckConstraint("CK_Lanes_Name_NotEmpty_sqlite", "length(trim(Name)) > 0");
                    t.HasCheckConstraint("CK_Lanes_Order_NonNegative_sqlite", "[Order] >= 0");
                });

                e.Property(l => l.RowVersion)
                    .IsRequired()
                    .IsConcurrencyToken()
                    .ValueGeneratedOnAddOrUpdate()
                    .HasDefaultValueSql("randomblob(8)");
            });

            modelBuilder.Entity<Project>(e =>
            {
                e.ToTable("Projects", t =>
                {
                    t.HasCheckConstraint("CK_Projects_Name_NotEmpty_sqlite", "length(trim(Name)) > 0");
                    t.HasCheckConstraint("CK_Projects_UpdatedAt_GTE_CreatedAt_sqlite", "UpdatedAt >= CreatedAt");
                    t.HasCheckConstraint("CK_Projects_Slug_Lowercase_sqlite", "Slug = lower(Slug)");
                    t.HasCheckConstraint("CK_Projects_Slug_NoSpaces_sqlite", "Slug NOT LIKE '% %'");
                    t.HasCheckConstraint("CK_Projects_Slug_NoDoubleDash_sqlite", "Slug NOT LIKE '%--%'");
                    t.HasCheckConstraint("CK_Projects_Slug_NoLeadingDash_sqlite", "Slug NOT LIKE '-%'");
                    t.HasCheckConstraint("CK_Projects_Slug_NoTrailingDash_sqlite", "Slug NOT LIKE '%-'");
                });

                e.Property(p => p.RowVersion)
                    .IsRequired()
                    .IsConcurrencyToken()
                    .ValueGeneratedOnAddOrUpdate()
                    .HasDefaultValueSql("randomblob(8)");

                var dtoToLong = new ValueConverter<DateTimeOffset, long>(
                    v => v.ToUnixTimeMilliseconds(),
                    v => DateTimeOffset.FromUnixTimeMilliseconds(v));

                e.Property(p => p.CreatedAt).HasConversion(dtoToLong);
                e.Property(p => p.UpdatedAt).HasConversion(dtoToLong);
            });

            modelBuilder.Entity<ProjectMember>(e =>
            {
                e.ToTable("ProjectMembers", t =>
                {
                    t.HasCheckConstraint(
                        "CK_ProjectMembers_RemovedAt_After_JoinedAt_sqlite",
                        "RemovedAt IS NULL OR RemovedAt >= JoinedAt");
                });

                e.Property(m => m.RowVersion)
                    .IsRequired()
                    .IsConcurrencyToken()
                    .ValueGeneratedOnAddOrUpdate()
                    .HasDefaultValueSql("randomblob(8)");
            });

            modelBuilder.Entity<TaskActivity>(e =>
            {
                e.ToTable("TaskActivities", t =>
                {
                    t.HasCheckConstraint("CK_TaskActivities_Payload_NotEmpty_sqlite", "length(trim(Payload)) > 0");
                });
            });

            modelBuilder.Entity<TaskItem>(e =>
            {
                e.ToTable("Tasks", t =>
                {
                    t.HasCheckConstraint("CK_Tasks_Title_NotEmpty_sqlite", "length(trim(Title)) > 0");
                    t.HasCheckConstraint("CK_Tasks_Description_NotEmpty_sqlite", "length(trim(Description)) > 0");
                    t.HasCheckConstraint("CK_Tasks_SortKey_NonNegative_sqlite", "[SortKey] >= 0");
                });

                e.Property(t => t.RowVersion)
                    .IsRequired()
                    .IsConcurrencyToken()
                    .ValueGeneratedOnAddOrUpdate()
                    .HasDefaultValueSql("randomblob(8)");

                e.Property(t => t.SortKey)
                    .HasConversion<double>()
                    .HasColumnType("REAL");
            });

            modelBuilder.Entity<TaskNote>(e =>
            {
                e.ToTable("Notes", t =>
                {
                    t.HasCheckConstraint("CK_Notes_Content_NotEmpty_sqlite", "length(trim(Content)) > 0");
                });

                e.Property(n => n.RowVersion)
                    .IsRequired()
                    .IsConcurrencyToken()
                    .ValueGeneratedOnAddOrUpdate()
                    .HasDefaultValueSql("randomblob(8)");

                var dtoToLong = new ValueConverter<DateTimeOffset, long>(
                    v => v.ToUnixTimeMilliseconds(),
                    v => DateTimeOffset.FromUnixTimeMilliseconds(v));

                e.Property(n => n.CreatedAt).HasConversion(dtoToLong);
                e.Property(n => n.UpdatedAt).HasConversion(dtoToLong);
            });

            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("Users", t =>
                {
                    t.HasCheckConstraint("CK_Users_Email_NotEmpty_sqlite", "length(trim(Email)) > 0");
                    t.HasCheckConstraint("CK_Users_Name_NotEmpty_sqlite", "length(trim(Name)) > 0");
                    t.HasCheckConstraint("CK_Users_PasswordHash_Length_32_sqlite", "length([PasswordHash]) = 32");
                    t.HasCheckConstraint("CK_Users_PasswordSalt_Length_16_sqlite", "length([PasswordSalt]) = 16");
                });

                e.Property(u => u.RowVersion)
                    .IsRequired()
                    .IsConcurrencyToken()
                    .ValueGeneratedOnAddOrUpdate()
                    .HasDefaultValueSql("randomblob(8)");
            });
        }
    }
}
